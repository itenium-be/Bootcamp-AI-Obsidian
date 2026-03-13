using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/roadmap")]
[Authorize]
public class RoadmapController : ControllerBase
{
    private const int DefaultNodeLimit = 12;

    private readonly AppDbContext _db;

    public RoadmapController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get personalised roadmap for a consultant.
    /// FR12: Only skills in the consultant's assigned competence centre profile are returned.
    /// FR13: Default shows 8-12 nodes (progressive disclosure).
    /// FR14: showAll=true reveals the complete profile roadmap.
    /// GET /api/roadmap?userId={id}
    /// GET /api/roadmap?userId={id}&amp;showAll=true
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<RoadmapResponse>> GetRoadmap(
        [FromQuery] string userId,
        [FromQuery] bool showAll = false)
    {
        // Look up consultant's profile assignment
        var profile = await _db.ConsultantProfiles
            .FirstOrDefaultAsync(cp => cp.UserId == userId);

        if (profile == null)
        {
            return Ok(new RoadmapResponse
            {
                UserId = userId,
                Nodes = [],
                TotalSkillCount = 0,
            });
        }

        // Get all skills for this profile, ordered by sort order
        var allSkillsQuery = _db.SkillProfiles
            .Where(sp => sp.Profile == profile.Profile)
            .OrderBy(sp => sp.SortOrder)
            .Select(sp => sp.Skill);

        var totalCount = await allSkillsQuery.CountAsync();

        // Get active goals for this consultant (for status/niveau overlay)
        var goals = await _db.Goals
            .Where(g => g.ConsultantId == userId && g.Status == GoalStatus.Active)
            .ToListAsync();

        var goalsLookup = goals.ToDictionary(g => g.SkillId);

        IQueryable<SkillEntity> pagedQuery = allSkillsQuery;

        if (!showAll)
        {
            pagedQuery = pagedQuery.Take(DefaultNodeLimit);
        }

        var skills = await pagedQuery.ToListAsync();

        var nodes = skills.Select(s =>
        {
            goalsLookup.TryGetValue(s.Id, out var goal);
            var currentNiveau = goal?.CurrentNiveau ?? 0;
            var targetNiveau = goal?.TargetNiveau;

            var status = currentNiveau >= s.LevelCount
                ? RoadmapNodeStatus.Complete
                : goal != null
                    ? RoadmapNodeStatus.Active
                    : RoadmapNodeStatus.Locked;

            return new RoadmapNode
            {
                SkillId = s.Id,
                SkillName = s.Name,
                Category = s.Category,
                LevelCount = s.LevelCount,
                CurrentNiveau = currentNiveau,
                TargetNiveau = targetNiveau,
                Status = status,
                PrerequisiteWarnings = BuildPrerequisiteWarnings(s.Prerequisites, goalsLookup),
            };
        }).ToList();

        return Ok(new RoadmapResponse
        {
            UserId = userId,
            Profile = profile.Profile,
            Nodes = nodes,
            TotalSkillCount = totalCount,
            ShowAll = showAll,
        });
    }

    private static List<PrerequisiteWarning> BuildPrerequisiteWarnings(
        IList<SkillPrerequisite> prerequisites,
        Dictionary<int, GoalEntity> goalsLookup)
    {
        if (prerequisites.Count == 0)
        {
            return [];
        }

        // FR8: Non-blocking — we just surface warnings, never lock the skill
        return prerequisites
            .Where(p =>
            {
                goalsLookup.TryGetValue(p.SkillId, out var g);
                return g == null || g.CurrentNiveau < p.RequiredNiveau;
            })
            .Select(p => new PrerequisiteWarning
            {
                SkillId = p.SkillId,
                SkillName = $"Skill#{p.SkillId}",
                RequiredNiveau = p.RequiredNiveau,
                WarningText = $"Skill#{p.SkillId} niveau {p.RequiredNiveau} not yet met — you can explore this skill, but your coach may ask you to address prerequisites first",
            })
            .ToList();
    }
}

// ── Response models ───────────────────────────────────────────────────────────

public class RoadmapResponse
{
    public required string UserId { get; set; }
    public CompetenceCentreProfile? Profile { get; set; }
    public IList<RoadmapNode> Nodes { get; set; } = [];
    public int TotalSkillCount { get; set; }
    public bool ShowAll { get; set; }
}

public class RoadmapNode
{
    public int SkillId { get; set; }
    public required string SkillName { get; set; }
    public required string Category { get; set; }
    public int LevelCount { get; set; }
    public int CurrentNiveau { get; set; }
    public int? TargetNiveau { get; set; }
    public RoadmapNodeStatus Status { get; set; }
    public IList<PrerequisiteWarning> PrerequisiteWarnings { get; set; } = [];
}

public enum RoadmapNodeStatus
{
    Active,
    Locked,
    Complete,
}
