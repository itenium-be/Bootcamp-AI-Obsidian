using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

public record CreateSkillRequest(
    string Name,
    string? Description,
    string? Category,
    int LevelCount,
    bool IsUniversal,
    IReadOnlyList<CreateLevelDescriptorRequest> LevelDescriptors);

public record UpdateSkillRequest(
    string Name,
    string? Description,
    string? Category,
    int LevelCount,
    bool IsUniversal);

public record CreateLevelDescriptorRequest(int Level, string Description);

[ApiController]
[Route("api/skill")]
[Authorize]
public class SkillController : ControllerBase
{
    private readonly AppDbContext _db;

    public SkillController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all skills in the catalogue.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SkillEntity>>> GetSkills()
    {
        var skills = await _db.Skills
            .Include(s => s.LevelDescriptors)
            .ToListAsync();
        return Ok(skills);
    }

    /// <summary>
    /// Get a skill by ID including level descriptors and prerequisites.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SkillEntity>> GetSkill(int id)
    {
        var skill = await _db.Skills
            .Include(s => s.LevelDescriptors)
            .Include(s => s.Prerequisites)
                .ThenInclude(p => p.PrerequisiteSkill)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (skill == null)
        {
            return NotFound();
        }

        return Ok(skill);
    }

    /// <summary>
    /// Create a new skill.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SkillEntity>> CreateSkill([FromBody] CreateSkillRequest request)
    {
        var skill = new SkillEntity
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            LevelCount = request.LevelCount,
            IsUniversal = request.IsUniversal,
            LevelDescriptors = request.LevelDescriptors
                .Select(d => new SkillLevelDescriptorEntity { Level = d.Level, Description = d.Description })
                .ToList(),
        };

        _db.Skills.Add(skill);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, skill);
    }

    /// <summary>
    /// Update an existing skill.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SkillEntity>> UpdateSkill(int id, [FromBody] UpdateSkillRequest request)
    {
        var skill = await _db.Skills.FindAsync(id);
        if (skill == null)
        {
            return NotFound();
        }

        skill.Name = request.Name;
        skill.Description = request.Description;
        skill.Category = request.Category;
        skill.LevelCount = request.LevelCount;
        skill.IsUniversal = request.IsUniversal;

        await _db.SaveChangesAsync();
        return Ok(skill);
    }

    /// <summary>
    /// Delete a skill.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteSkill(int id)
    {
        var skill = await _db.Skills.FindAsync(id);
        if (skill == null)
        {
            return NotFound();
        }

        _db.Skills.Remove(skill);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Check unmet prerequisites for a skill for a given user (non-blocking warning).
    /// </summary>
    [HttpGet("{id:int}/prerequisite-check")]
    public async Task<ActionResult<IReadOnlyList<PrerequisiteCheckItem>>> CheckPrerequisites(int id, [FromQuery] string userId)
    {
        var prerequisites = await _db.SkillPrerequisites
            .Where(p => p.SkillId == id)
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

public record PrerequisiteCheckItem(string SkillName, int RequiredLevel, int CurrentLevel);
