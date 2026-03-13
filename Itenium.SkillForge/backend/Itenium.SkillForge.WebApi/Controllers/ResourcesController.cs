using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResourcesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _currentUser;

    public ResourcesController(AppDbContext db, ISkillForgeUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Browse resources, filterable by skill and niveau range (FR21).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ResourceDto>>> GetResources(
        [FromQuery] int? skillId,
        [FromQuery] int? fromNiveau,
        [FromQuery] int? toNiveau)
    {
        var query = _db.Resources.AsQueryable();

        if (skillId.HasValue)
        {
            query = query.Where(r => r.SkillId == skillId.Value);
        }

        if (fromNiveau.HasValue)
        {
            query = query.Where(r => r.ToNiveau >= fromNiveau.Value);
        }

        if (toNiveau.HasValue)
        {
            query = query.Where(r => r.FromNiveau <= toNiveau.Value);
        }

        var resources = await query
            .OrderByDescending(r => r.ContributedAt)
            .Select(r => new ResourceDto
            {
                Id = r.Id,
                Title = r.Title,
                Url = r.Url,
                Type = r.Type.ToString(),
                SkillId = r.SkillId,
                FromNiveau = r.FromNiveau,
                ToNiveau = r.ToNiveau,
                ContributedBy = r.ContributedBy,
                ContributedAt = r.ContributedAt,
                ThumbsUp = r.ThumbsUp,
                ThumbsDown = r.ThumbsDown,
            })
            .ToListAsync();

        return Ok(resources);
    }

    /// <summary>
    /// Get a single resource by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResourceDto>> GetResource(Guid id)
    {
        var resource = await _db.Resources.FindAsync(id);
        if (resource == null)
        {
            return NotFound();
        }

        return Ok(new ResourceDto
        {
            Id = resource.Id,
            Title = resource.Title,
            Url = resource.Url,
            Type = resource.Type.ToString(),
            SkillId = resource.SkillId,
            FromNiveau = resource.FromNiveau,
            ToNiveau = resource.ToNiveau,
            ContributedBy = resource.ContributedBy,
            ContributedAt = resource.ContributedAt,
            ThumbsUp = resource.ThumbsUp,
            ThumbsDown = resource.ThumbsDown,
        });
    }

    /// <summary>
    /// Contribute a resource to the library (FR22).
    /// Any authenticated user can add a resource.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResourceDto>> CreateResource([FromBody] CreateResourceRequest request)
    {
        var resource = new ResourceEntity
        {
            Title = request.Title,
            Url = request.Url,
            Type = request.Type,
            SkillId = request.SkillId,
            FromNiveau = request.FromNiveau,
            ToNiveau = request.ToNiveau,
            ContributedBy = _currentUser.Id ?? string.Empty,
        };

        _db.Resources.Add(resource);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, new ResourceDto
        {
            Id = resource.Id,
            Title = resource.Title,
            Url = resource.Url,
            Type = resource.Type.ToString(),
            SkillId = resource.SkillId,
            FromNiveau = resource.FromNiveau,
            ToNiveau = resource.ToNiveau,
            ContributedBy = resource.ContributedBy,
            ContributedAt = resource.ContributedAt,
            ThumbsUp = resource.ThumbsUp,
            ThumbsDown = resource.ThumbsDown,
        });
    }

    /// <summary>
    /// Mark a resource as completed, linked to a goal as evidence (FR23, FR30).
    /// Learner role.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "learner")]
    public async Task<ActionResult<ResourceCompletionDto>> CompleteResource(Guid id, [FromBody] CompleteResourceRequest request)
    {
        var resource = await _db.Resources.FindAsync(id);
        if (resource == null)
        {
            return NotFound();
        }

        var goal = await _db.Goals.FindAsync(request.GoalId);
        if (goal == null)
        {
            return NotFound(new { message = "Goal not found" });
        }

        if (goal.ConsultantId != (_currentUser.Id ?? string.Empty))
        {
            return Forbid();
        }

        var completion = new ResourceCompletionEntity
        {
            ResourceId = id,
            ConsultantId = _currentUser.Id ?? string.Empty,
            GoalId = request.GoalId,
        };

        _db.ResourceCompletions.Add(completion);
        await _db.SaveChangesAsync();

        return Ok(new ResourceCompletionDto
        {
            Id = completion.Id,
            ResourceId = completion.ResourceId,
            ConsultantId = completion.ConsultantId,
            GoalId = completion.GoalId,
            CompletedAt = completion.CompletedAt,
        });
    }

    /// <summary>
    /// Rate a resource with thumbs up or thumbs down (FR24).
    /// Any authenticated user. One rating per user per resource — upsert.
    /// </summary>
    [HttpPost("{id:guid}/rate")]
    public async Task<ActionResult> RateResource(Guid id, [FromBody] RateResourceRequest request)
    {
        var resource = await _db.Resources.FindAsync(id);
        if (resource == null)
        {
            return NotFound();
        }

        var userId = _currentUser.Id ?? string.Empty;
        var rating = request.Rating == "up" ? ResourceRating.ThumbsUp : ResourceRating.ThumbsDown;

        var existing = await _db.ResourceRatings
            .FirstOrDefaultAsync(r => r.ResourceId == id && r.UserId == userId);

        if (existing != null)
        {
            var oldRating = existing.Rating;
            existing.Rating = rating;
            existing.RatedAt = DateTime.UtcNow;

            // Update denormalized counts on resource
            if (oldRating == ResourceRating.ThumbsUp) resource.ThumbsUp = Math.Max(0, resource.ThumbsUp - 1);
            else resource.ThumbsDown = Math.Max(0, resource.ThumbsDown - 1);
        }
        else
        {
            _db.ResourceRatings.Add(new ResourceRatingEntity
            {
                ResourceId = id,
                UserId = userId,
                Rating = rating,
            });
        }

        if (rating == ResourceRating.ThumbsUp) resource.ThumbsUp++;
        else resource.ThumbsDown++;

        await _db.SaveChangesAsync();

        return Ok(new { thumbsUp = resource.ThumbsUp, thumbsDown = resource.ThumbsDown });
    }
}

public record ResourceDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string Url { get; init; } = "";
    public string Type { get; init; } = "";
    public int SkillId { get; init; }
    public int FromNiveau { get; init; }
    public int ToNiveau { get; init; }
    public string ContributedBy { get; init; } = "";
    public DateTime ContributedAt { get; init; }
    public int ThumbsUp { get; init; }
    public int ThumbsDown { get; init; }
}

public record CreateResourceRequest(
    string Title,
    string Url,
    ResourceType Type,
    int SkillId,
    int FromNiveau,
    int ToNiveau);

public record CompleteResourceRequest(Guid GoalId);

public record RateResourceRequest(string Rating);

public record ResourceCompletionDto
{
    public Guid Id { get; init; }
    public Guid ResourceId { get; init; }
    public string ConsultantId { get; init; } = "";
    public Guid GoalId { get; init; }
    public DateTime CompletedAt { get; init; }
}
