using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

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
    /// Get the full seniority threshold ruleset for a competence centre profile.
    /// FR38: Thresholds computed at read time — no background jobs.
    /// GET /api/seniority/{profile}
    /// </summary>
    [HttpGet("{profile}")]
    public async Task<ActionResult<SeniorityRulesetResponse>> GetThresholds(CompetenceCentreProfile profile)
    {
        var thresholds = await _db.SeniorityThresholds
            .Include(t => t.Skill)
            .Where(t => t.Profile == profile)
            .OrderBy(t => t.SeniorityLevel)
            .ThenBy(t => t.SkillId)
            .ToListAsync();

        var response = new SeniorityRulesetResponse
        {
            Profile = profile,
            Thresholds = thresholds.Select(t => new SeniorityThresholdDto
            {
                Id = t.Id,
                SeniorityLevel = t.SeniorityLevel,
                SkillId = t.SkillId,
                SkillName = t.Skill?.Name ?? string.Empty,
                MinNiveau = t.MinNiveau,
            }).ToList(),
        };

        return Ok(response);
    }

    /// <summary>
    /// Get seniority progress for a consultant against their profile's thresholds.
    /// FR39: "You meet X/Y [Junior|Medior|Senior] requirements"
    /// Computed at read time — no background jobs.
    /// GET /api/seniority/progress?userId={id}
    /// </summary>
    [HttpGet("progress")]
    public async Task<ActionResult<SeniorityProgressResponse>> GetProgress([FromQuery] string userId)
    {
        var profile = await _db.ConsultantProfiles
            .FirstOrDefaultAsync(cp => cp.UserId == userId);

        if (profile == null)
        {
            return Ok(new SeniorityProgressResponse
            {
                UserId = userId,
                MetCount = 0,
                RequiredCount = 0,
                UnmetRequirements = [],
            });
        }

        // Get the consultant's current niveaux from Goals
        var goals = await _db.Goals
            .Where(g => g.ConsultantId == userId)
            .ToListAsync();
        var niveauBySkill = goals.ToDictionary(g => g.SkillId, g => g.CurrentNiveau);

        // Determine current seniority level and next level to target
        var allThresholds = await _db.SeniorityThresholds
            .Include(t => t.Skill)
            .Where(t => t.Profile == profile.Profile)
            .ToListAsync();

        // Determine which seniority level we are targeting
        // Start with Junior; if all Junior met → target Medior; if Medior met → target Senior
        var targetLevel = DetermineTargetLevel(allThresholds, niveauBySkill);

        var targetThresholds = allThresholds
            .Where(t => t.SeniorityLevel == targetLevel)
            .ToList();

        var metThresholds = targetThresholds
            .Where(t => niveauBySkill.TryGetValue(t.SkillId, out var niveau) && niveau >= t.MinNiveau)
            .ToList();

        var unmetThresholds = targetThresholds
            .Except(metThresholds)
            .Select(t => new UnmetRequirement
            {
                SkillId = t.SkillId,
                SkillName = t.Skill?.Name ?? $"Skill#{t.SkillId}",
                MinNiveau = t.MinNiveau,
                CurrentNiveau = niveauBySkill.GetValueOrDefault(t.SkillId, 0),
            })
            .ToList();

        return Ok(new SeniorityProgressResponse
        {
            UserId = userId,
            Profile = profile.Profile,
            CurrentLevel = GetCurrentLevel(allThresholds, niveauBySkill),
            TargetLevel = targetLevel,
            MetCount = metThresholds.Count,
            RequiredCount = targetThresholds.Count,
            UnmetRequirements = unmetThresholds,
        });
    }

    private static SeniorityLevel DetermineTargetLevel(
        List<SeniorityThresholdEntity> allThresholds,
        Dictionary<int, int> niveauBySkill)
    {
        if (AllMet(allThresholds, SeniorityLevel.Junior, niveauBySkill) &&
            AllMet(allThresholds, SeniorityLevel.Medior, niveauBySkill))
        {
            return SeniorityLevel.Senior;
        }

        if (AllMet(allThresholds, SeniorityLevel.Junior, niveauBySkill))
        {
            return SeniorityLevel.Medior;
        }

        return SeniorityLevel.Junior;
    }

    private static SeniorityLevel? GetCurrentLevel(
        List<SeniorityThresholdEntity> allThresholds,
        Dictionary<int, int> niveauBySkill)
    {
        if (AllMet(allThresholds, SeniorityLevel.Senior, niveauBySkill))
        {
            return SeniorityLevel.Senior;
        }

        if (AllMet(allThresholds, SeniorityLevel.Medior, niveauBySkill))
        {
            return SeniorityLevel.Medior;
        }

        if (AllMet(allThresholds, SeniorityLevel.Junior, niveauBySkill))
        {
            return SeniorityLevel.Junior;
        }

        return null;
    }

    private static bool AllMet(
        List<SeniorityThresholdEntity> allThresholds,
        SeniorityLevel level,
        Dictionary<int, int> niveauBySkill)
    {
        var levelThresholds = allThresholds.Where(t => t.SeniorityLevel == level).ToList();
        if (levelThresholds.Count == 0) return false;
        return levelThresholds.All(t => niveauBySkill.TryGetValue(t.SkillId, out var n) && n >= t.MinNiveau);
    }
}

// ── Response models ───────────────────────────────────────────────────────────

public class SeniorityRulesetResponse
{
    public CompetenceCentreProfile Profile { get; set; }
    public List<SeniorityThresholdDto> Thresholds { get; set; } = [];
}

public class SeniorityThresholdDto
{
    public int Id { get; set; }
    public SeniorityLevel SeniorityLevel { get; set; }
    public int SkillId { get; set; }
    public required string SkillName { get; set; }
    public int MinNiveau { get; set; }
}

public class SeniorityProgressResponse
{
    public required string UserId { get; set; }
    public CompetenceCentreProfile? Profile { get; set; }
    public SeniorityLevel? CurrentLevel { get; set; }
    public SeniorityLevel? TargetLevel { get; set; }

    /// <summary>
    /// Number of met threshold requirements toward target level.
    /// FR39: "You meet X/Y [Junior|Medior|Senior] requirements"
    /// </summary>
    public int MetCount { get; set; }

    public int RequiredCount { get; set; }
    public List<UnmetRequirement> UnmetRequirements { get; set; } = [];
}

public class UnmetRequirement
{
    public int SkillId { get; set; }
    public required string SkillName { get; set; }
    public int MinNiveau { get; set; }
    public int CurrentNiveau { get; set; }
}
