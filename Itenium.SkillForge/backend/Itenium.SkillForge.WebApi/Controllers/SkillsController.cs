using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/skills")]
[Authorize]
public class SkillsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SkillsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all skills, optionally filtered by competence centre profile.
    /// GET /api/skills
    /// GET /api/skills?profile=DotNet
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SkillResponse>>> GetSkills([FromQuery] CompetenceCentreProfile? profile)
    {
        IQueryable<SkillEntity> query = _db.Skills;

        if (profile.HasValue)
        {
            query = query
                .Where(s => s.SkillProfiles.Any(sp => sp.Profile == profile.Value));
        }

        var skills = await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .Select(s => new SkillResponse
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
                LevelCount = s.LevelCount,
                LevelDescriptorsJson = s.LevelDescriptorsJson,
                PrerequisitesJson = s.PrerequisitesJson,
                CreatedAt = s.CreatedAt,
                Profiles = s.SkillProfiles.Select(sp => sp.Profile).ToList(),
            })
            .ToListAsync();

        return Ok(skills);
    }

    /// <summary>
    /// Get a single skill by ID, including non-blocking prerequisite warning metadata.
    /// GET /api/skills/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SkillDetailResponse>> GetSkill(int id)
    {
        var skill = await _db.Skills
            .Include(s => s.SkillProfiles)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (skill == null)
        {
            return NotFound();
        }

        // Build prerequisite warning metadata (FR8 / Story #17)
        // Warnings are informational — skill is never locked.
        var prereqs = skill.Prerequisites;
        var prereqWarnings = new List<PrerequisiteWarning>();

        if (prereqs.Count > 0)
        {
            var prereqSkillIds = prereqs.Select(p => p.SkillId).ToList();
            var prereqSkills = await _db.Skills
                .Where(s => prereqSkillIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            prereqWarnings = prereqs.Select(p =>
            {
                var prereqSkill = prereqSkills.FirstOrDefault(s => s.Id == p.SkillId);
                return new PrerequisiteWarning
                {
                    SkillId = p.SkillId,
                    SkillName = prereqSkill?.Name ?? "Unknown",
                    RequiredNiveau = p.RequiredNiveau,
                    WarningText = $"{prereqSkill?.Name ?? "Unknown"} niveau {p.RequiredNiveau} not yet met — you can explore this skill, but your coach may ask you to address prerequisites first",
                };
            }).ToList();
        }

        var response = new SkillDetailResponse
        {
            Id = skill.Id,
            Name = skill.Name,
            Category = skill.Category,
            Description = skill.Description,
            LevelCount = skill.LevelCount,
            LevelDescriptors = skill.LevelDescriptors,
            Prerequisites = skill.Prerequisites,
            PrerequisiteWarnings = prereqWarnings,
            CreatedAt = skill.CreatedAt,
            Profiles = skill.SkillProfiles.Select(sp => sp.Profile).ToList(),
        };

        return Ok(response);
    }
}

// ── Response models ──────────────────────────────────────────────────────────

public class SkillResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public string? Description { get; set; }
    public int LevelCount { get; set; }
    public string LevelDescriptorsJson { get; set; } = "[]";
    public string PrerequisitesJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
    public List<CompetenceCentreProfile> Profiles { get; set; } = [];
}

public class SkillDetailResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public string? Description { get; set; }
    public int LevelCount { get; set; }
    public List<string> LevelDescriptors { get; set; } = [];
    public List<SkillPrerequisite> Prerequisites { get; set; } = [];

    /// <summary>
    /// Non-blocking warnings for unmet prerequisites (FR8).
    /// Skill is NEVER locked — these are informational only.
    /// </summary>
    public List<PrerequisiteWarning> PrerequisiteWarnings { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public List<CompetenceCentreProfile> Profiles { get; set; } = [];
}

public class PrerequisiteWarning
{
    public int SkillId { get; set; }
    public required string SkillName { get; set; }
    public int RequiredNiveau { get; set; }
    public required string WarningText { get; set; }
}
