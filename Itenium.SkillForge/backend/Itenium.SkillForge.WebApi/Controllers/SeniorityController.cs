using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

public record SeniorityProgressItem(string Level, int Met, int Required);

[ApiController]
[Route("api/seniority")]
[Authorize]
public class SeniorityController : ControllerBase
{
    private readonly AppDbContext _db;

    public SeniorityController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get seniority progress for a consultant.
    /// Returns met/required counts for each seniority level (Junior, Medior, Senior).
    /// Computed at read time.
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<IReadOnlyList<SeniorityProgressItem>>> GetProgress(string userId)
    {
        var profile = await _db.ConsultantProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return Ok(BuildEmptyProgress());
        }

        var thresholds = await _db.SeniorityThresholds
            .Where(t => t.TeamId == profile.TeamId)
            .ToListAsync();

        var progressMap = await _db.ConsultantSkillProgress
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.SkillId, p => p.AchievedLevel);

        var levels = new[] { SeniorityLevel.Junior, SeniorityLevel.Medior, SeniorityLevel.Senior };

        var result = levels.Select(level =>
        {
            var levelThresholds = thresholds.Where(t => t.SeniorityLevel == level).ToList();
            var met = levelThresholds.Count(t =>
            {
                var achieved = progressMap.GetValueOrDefault(t.SkillId, 0);
                return achieved >= t.MinimumLevel;
            });

            return new SeniorityProgressItem(level.ToString(), met, levelThresholds.Count);
        }).ToList();

        return Ok(result);
    }

    private static List<SeniorityProgressItem> BuildEmptyProgress()
    {
        return
        [
            new SeniorityProgressItem("Junior", 0, 0),
            new SeniorityProgressItem("Medior", 0, 0),
            new SeniorityProgressItem("Senior", 0, 0),
        ];
    }
}
