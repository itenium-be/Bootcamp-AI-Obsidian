using Itenium.SkillForge.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Coach dashboard overview (FR25–FR29).
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private const int InactivityThresholdDays = 21; // FR27: 3 weeks

    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get the team overview for a coach (manager/backoffice role). FR25–FR29.
    /// GET /api/dashboard/coach?coachId={id}
    /// </summary>
    [HttpGet("coach")]
    [Authorize(Roles = "manager,backoffice")]
    public async Task<ActionResult<List<CoachDashboardItem>>> GetCoachDashboard([FromQuery] string coachId)
    {
        var now = DateTime.UtcNow;
        var inactivityCutoff = now.AddDays(-InactivityThresholdDays);

        // Get all goals assigned by this coach, grouped by consultant
        var goals = await _db.Goals
            .Where(g => g.CoachId == coachId)
            .ToListAsync();

        var consultantGroups = goals
            .GroupBy(g => g.ConsultantId, StringComparer.Ordinal)
            .ToList();

        // Get all active readiness flags for these consultants
        var consultantIds = consultantGroups.Select(g => g.Key).ToList();
        var activeFlags = await _db.ReadinessFlags
            .Where(rf => rf.IsActive && consultantIds.Contains(rf.ConsultantId))
            .ToListAsync();

        var flagsByConsultant = activeFlags
            .GroupBy(rf => rf.ConsultantId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.OrderBy(rf => rf.RaisedAt).First(), StringComparer.Ordinal);

        var items = consultantGroups.Select(group =>
        {
            var consultantGoals = group.ToList();
            var activeGoalCount = consultantGoals.Count(g => g.Status == Entities.GoalStatus.Active);

            // FR27: last activity = most recent goal creation or flag raise
            var lastGoalActivity = consultantGoals.Max(g => g.CreatedAt);
            flagsByConsultant.TryGetValue(group.Key, out var activeFlag);
            var lastFlagActivity = activeFlag?.RaisedAt ?? DateTime.MinValue;
            var lastActivityAt = lastGoalActivity > lastFlagActivity ? lastGoalActivity : lastFlagActivity;

            // FR26: readiness flag age in days
            double? flagAgeInDays = activeFlag != null
                ? (now - activeFlag.RaisedAt).TotalDays
                : null;

            return new CoachDashboardItem
            {
                ConsultantId = group.Key,
                ActiveGoalCount = activeGoalCount,
                ReadinessFlagAgeInDays = flagAgeInDays,
                LastActivityAt = lastActivityAt,
                IsInactive = lastActivityAt < inactivityCutoff, // FR27
            };
        }).ToList();

        return Ok(items);
    }
}

// ── Response models ───────────────────────────────────────────────────────────

public class CoachDashboardItem
{
    public required string ConsultantId { get; set; }

    /// <summary>
    /// FR28: Active goal count per consultant.
    /// </summary>
    public int ActiveGoalCount { get; set; }

    /// <summary>
    /// FR26: Age of the oldest active readiness flag in days, null if no active flag.
    /// </summary>
    public double? ReadinessFlagAgeInDays { get; set; }

    /// <summary>
    /// FR27: Date of last recorded activity (goal creation or readiness flag).
    /// </summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// FR27: True if no activity has been recorded in the past 3 weeks.
    /// </summary>
    public bool IsInactive { get; set; }
}
