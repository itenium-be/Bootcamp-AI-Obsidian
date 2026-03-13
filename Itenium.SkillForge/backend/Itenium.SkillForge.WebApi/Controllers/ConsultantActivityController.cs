using Itenium.SkillForge.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Consultant full activity history (FR30).
/// Story #30 — Coach views consultant full activity history.
/// Uses route prefix /api/consultants to match ConsultantsController (ASP.NET Core merges them correctly).
/// </summary>
[ApiController]
[Route("api/consultants")]
[Authorize]
public class ConsultantActivityController : ControllerBase
{
    private readonly AppDbContext _db;

    public ConsultantActivityController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get chronological activity feed for a consultant (manager/backoffice role). FR30.
    /// Returns: goals created, readiness flags raised — ordered descending by time.
    /// GET /api/consultants/{id}/activity
    /// </summary>
    [HttpGet("{id}/activity")]
    [Authorize(Roles = "manager,backoffice,learner")]
    public async Task<ActionResult<List<ActivityFeedItem>>> GetActivity(string id)
    {
        var items = new List<ActivityFeedItem>();

        // Goals created
        var goals = await _db.Goals
            .Include(g => g.Skill)
            .Where(g => g.ConsultantId == id)
            .ToListAsync();

        items.AddRange(goals.Select(g => new ActivityFeedItem
        {
            ConsultantId = id,
            Type = ActivityType.GoalCreated,
            OccurredAt = g.CreatedAt,
            Description = $"Goal created: {g.Skill?.Name ?? $"Skill#{g.SkillId}"} (niveau {g.CurrentNiveau} → {g.TargetNiveau})",
            ReferenceId = g.Id.ToString(),
        }));

        // Readiness flags raised
        var goalIds = goals.Select(g => g.Id).ToList();
        var flags = await _db.ReadinessFlags
            .Where(rf => rf.ConsultantId == id || goalIds.Contains(rf.GoalId))
            .ToListAsync();

        items.AddRange(flags.Select(rf => new ActivityFeedItem
        {
            ConsultantId = id,
            Type = ActivityType.ReadinessFlagRaised,
            OccurredAt = rf.RaisedAt,
            Description = rf.IsActive ? "Readiness flag raised" : "Readiness flag raised (lowered)",
            ReferenceId = rf.GoalId.ToString(),
        }));

        // Return chronologically descending (newest first)
        return Ok(items.OrderByDescending(i => i.OccurredAt).ToList());
    }
}

// ── Response models ───────────────────────────────────────────────────────────

public class ActivityFeedItem
{
    public required string ConsultantId { get; set; }
    public ActivityType Type { get; set; }
    public DateTime OccurredAt { get; set; }
    public required string Description { get; set; }
    public string? ReferenceId { get; set; }
}

public enum ActivityType
{
    GoalCreated,
    ReadinessFlagRaised,
    ResourceCompleted,
    ValidationReceived,
}
