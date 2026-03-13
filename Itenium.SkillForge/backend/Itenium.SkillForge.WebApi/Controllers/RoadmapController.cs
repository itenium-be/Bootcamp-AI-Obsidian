using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

public record UpdateProgressRequest(int SkillId, int AchievedLevel);

public record RoadmapSkillItem(
    int SkillId,
    string Name,
    string? Category,
    int LevelCount,
    int AchievedLevel,
    IReadOnlyList<PrerequisiteCheckItem> UnmetPrerequisites);

public record RoadmapResponse(IReadOnlyList<RoadmapSkillItem> Skills);

[ApiController]
[Route("api/roadmap")]
[Authorize]
public class RoadmapController : ControllerBase
{
    private readonly AppDbContext _db;

    public RoadmapController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get the personalised roadmap for a consultant.
    /// Default returns up to 12 skills (anchors + next tier). Use showAll=true for all.
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<RoadmapResponse>> GetRoadmap(string userId, [FromQuery] bool showAll = false)
    {
        var profile = await _db.ConsultantProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return Ok(new RoadmapResponse([]));
        }

        // Load universal skills + any skills that have seniority thresholds for this team
        var teamSkillIds = await _db.SeniorityThresholds
            .Where(t => t.TeamId == profile.TeamId)
            .Select(t => t.SkillId)
            .Distinct()
            .ToListAsync();

        var skills = await _db.Skills
            .Include(s => s.LevelDescriptors)
            .Include(s => s.Prerequisites)
                .ThenInclude(p => p.PrerequisiteSkill)
            .Where(s => s.IsUniversal || teamSkillIds.Contains(s.Id))
            .ToListAsync();

        var progressMap = await _db.ConsultantSkillProgress
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.SkillId, p => p.AchievedLevel);

        var roadmapItems = skills.Select(skill =>
        {
            var achieved = progressMap.GetValueOrDefault(skill.Id, 0);

            var unmet = skill.Prerequisites
                .Where(p =>
                {
                    var prereqAchieved = progressMap.GetValueOrDefault(p.PrerequisiteSkillId, 0);
                    return prereqAchieved < p.RequiredLevel;
                })
                .Select(p => new PrerequisiteCheckItem(
                    p.PrerequisiteSkill.Name,
                    p.RequiredLevel,
                    progressMap.GetValueOrDefault(p.PrerequisiteSkillId, 0)))
                .ToList();

            return new RoadmapSkillItem(skill.Id, skill.Name, skill.Category, skill.LevelCount, achieved, unmet);
        }).ToList();

        if (!showAll)
        {
            // Default: return anchors (in-progress or not started) + next tier
            // Show skills not yet mastered, limited to 12
            roadmapItems = roadmapItems
                .OrderBy(r => r.AchievedLevel == r.LevelCount ? 1 : 0)  // mastered last
                .ThenBy(r => r.AchievedLevel)
                .Take(12)
                .ToList();
        }

        return Ok(new RoadmapResponse(roadmapItems));
    }

    /// <summary>
    /// Update a consultant's skill progress.
    /// </summary>
    [HttpPut("{userId}/progress")]
    public async Task<ActionResult> UpdateProgress(string userId, [FromBody] UpdateProgressRequest request)
    {
        var existing = await _db.ConsultantSkillProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SkillId == request.SkillId);

        if (existing == null)
        {
            _db.ConsultantSkillProgress.Add(new ConsultantSkillProgressEntity
            {
                UserId = userId,
                SkillId = request.SkillId,
                AchievedLevel = request.AchievedLevel,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.AchievedLevel = request.AchievedLevel;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Check unmet prerequisites for a skill for a given user (non-blocking warning).
    /// </summary>
    [HttpGet("{userId}/skill/{skillId:int}/prerequisite-check")]
    public async Task<ActionResult<List<PrerequisiteCheckItem>>> CheckPrerequisites(string userId, int skillId)
    {
        var prerequisites = await _db.SkillPrerequisites
            .Where(p => p.SkillId == skillId)
            .Include(p => p.PrerequisiteSkill)
            .ToListAsync();

        var progressMap = await _db.ConsultantSkillProgress
            .Where(p => p.UserId == userId && prerequisites.Select(pr => pr.PrerequisiteSkillId).Contains(p.SkillId))
            .ToDictionaryAsync(p => p.SkillId, p => p.AchievedLevel);

        var unmet = prerequisites
            .Where(p =>
            {
                var achieved = progressMap.GetValueOrDefault(p.PrerequisiteSkillId, 0);
                return achieved < p.RequiredLevel;
            })
            .Select(p => new PrerequisiteCheckItem(
                p.PrerequisiteSkill.Name,
                p.RequiredLevel,
                progressMap.GetValueOrDefault(p.PrerequisiteSkillId, 0)))
            .ToList();

        return Ok(unmet);
    }
}
