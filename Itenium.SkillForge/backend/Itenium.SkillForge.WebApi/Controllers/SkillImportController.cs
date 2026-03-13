using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

public record ImportSkillRequest(
    string Name,
    string? Description,
    string? Category,
    int LevelCount,
    bool IsUniversal,
    IReadOnlyList<ImportLevelDescriptorRequest> LevelDescriptors);

public record ImportLevelDescriptorRequest(int Level, string Description);

[ApiController]
[Route("api/admin/skills")]
[Authorize(Roles = "backoffice")]
public class SkillImportController : ControllerBase
{
    private readonly AppDbContext _db;

    public SkillImportController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Import skills from a JSON array. Idempotent: upserts by name.
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<ImportResult>> ImportSkills([FromBody] IReadOnlyList<ImportSkillRequest> skills)
    {
        var created = 0;
        var updated = 0;

        foreach (var item in skills)
        {
            var existing = await _db.Skills
                .Include(s => s.LevelDescriptors)
                .FirstOrDefaultAsync(s => s.Name == item.Name);

            if (existing == null)
            {
                var skill = new SkillEntity
                {
                    Name = item.Name,
                    Description = item.Description,
                    Category = item.Category,
                    LevelCount = item.LevelCount,
                    IsUniversal = item.IsUniversal,
                    LevelDescriptors = item.LevelDescriptors
                        .Select(d => new SkillLevelDescriptorEntity { Level = d.Level, Description = d.Description })
                        .ToList(),
                };
                _db.Skills.Add(skill);
                created++;
            }
            else
            {
                existing.Description = item.Description;
                existing.Category = item.Category;
                existing.LevelCount = item.LevelCount;
                existing.IsUniversal = item.IsUniversal;

                _db.SkillLevelDescriptors.RemoveRange(existing.LevelDescriptors);
                existing.LevelDescriptors = item.LevelDescriptors
                    .Select(d => new SkillLevelDescriptorEntity { Level = d.Level, Description = d.Description })
                    .ToList();

                updated++;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new ImportResult(created, updated));
    }
}

public record ImportResult(int Created, int Updated);
