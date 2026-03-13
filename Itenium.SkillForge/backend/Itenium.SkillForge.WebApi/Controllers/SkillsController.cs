using ClosedXML.Excel;
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
            LevelDescriptors = skill.LevelDescriptors.ToList(),
            Prerequisites = skill.Prerequisites.ToList(),
            PrerequisiteWarnings = prereqWarnings,
            CreatedAt = skill.CreatedAt,
            Profiles = skill.SkillProfiles.Select(sp => sp.Profile).ToList(),
        };

        return Ok(response);
    }

    /// <summary>
    /// Import skills from an Excel file (.xlsx).
    /// Expected columns: Name (required), Category (required), Description (optional),
    /// LevelCount (required, 1–10), Prerequisites (optional, comma-separated skill names).
    /// Existing skills with the same name are updated (upsert).
    /// POST /api/skills/import
    /// </summary>
    [HttpPost("import")]
    [Authorize(Roles = "backoffice")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<SkillImportResult>> ImportSkills(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded or file is empty.");
        }

        XLWorkbook workbook;
        try
        {
            using var stream = file.OpenReadStream();
            workbook = new XLWorkbook(stream);
        }
        catch (Exception)
        {
            return BadRequest("Could not parse the uploaded file as a valid Excel workbook.");
        }

        using (workbook)
        {
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                return BadRequest("The workbook contains no worksheets.");
            }

            // Read header row and map column names to indices (1-based)
            var headerRow = ws.Row(1);
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
            {
                headers[cell.GetString().Trim()] = cell.Address.ColumnNumber;
            }

            // Validate required columns
            var required = new[] { "Name", "Category", "LevelCount" };
            var missing = required.Where(c => !headers.ContainsKey(c)).ToList();
            if (missing.Count > 0)
            {
                return BadRequest($"Missing required columns: {string.Join(", ", missing)}");
            }

            int nameCol = headers["Name"];
            int categoryCol = headers["Category"];
            int levelCountCol = headers["LevelCount"];
            headers.TryGetValue("Description", out int descCol);
            headers.TryGetValue("Prerequisites", out int prereqCol);

            // Load existing skills by name for upsert
            var existingByName = await _db.Skills
                .ToDictionaryAsync(s => s.Name, StringComparer.OrdinalIgnoreCase);

            int imported = 0;
            int skipped = 0;
            var errors = new List<string>();

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            for (int row = 2; row <= lastRow; row++)
            {
                var name = ws.Cell(row, nameCol).GetString().Trim();
                var category = ws.Cell(row, categoryCol).GetString().Trim();
                var description = descCol > 0 ? ws.Cell(row, descCol).GetString().Trim() : null;
                var levelCountRaw = ws.Cell(row, levelCountCol).GetString().Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    skipped++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(category))
                {
                    errors.Add($"Row {row}: Category is required for skill '{name}'.");
                    skipped++;
                    continue;
                }

                if (!int.TryParse(levelCountRaw, out int levelCount) || levelCount < 1 || levelCount > 10)
                {
                    errors.Add($"Row {row}: LevelCount must be an integer between 1 and 10 for skill '{name}'.");
                    skipped++;
                    continue;
                }

                if (existingByName.TryGetValue(name, out var existing))
                {
                    // Update existing skill (upsert)
                    existing.Category = category;
                    existing.Description = string.IsNullOrWhiteSpace(description) ? existing.Description : description;
                    existing.LevelCount = levelCount;
                }
                else
                {
                    // Insert new skill
                    var skill = new SkillEntity
                    {
                        Name = name,
                        Category = category,
                        Description = string.IsNullOrWhiteSpace(description) ? null : description,
                        LevelCount = levelCount,
                    };
                    _db.Skills.Add(skill);
                    existingByName[name] = skill;
                }

                imported++;
            }

            await _db.SaveChangesAsync();

            // Second pass: resolve prerequisite links by name now that all skills have IDs
            if (prereqCol > 0)
            {
                for (int row = 2; row <= lastRow; row++)
                {
                    var name = ws.Cell(row, nameCol).GetString().Trim();
                    var prereqRaw = ws.Cell(row, prereqCol).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(prereqRaw))
                    {
                        continue;
                    }

                    if (!existingByName.TryGetValue(name, out var skill))
                    {
                        continue;
                    }

                    var prereqNames = prereqRaw
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    var resolvedPrereqs = new List<SkillPrerequisite>();
                    foreach (var prereqName in prereqNames)
                    {
                        if (existingByName.TryGetValue(prereqName, out var prereqSkill))
                        {
                            resolvedPrereqs.Add(new SkillPrerequisite { SkillId = prereqSkill.Id, RequiredNiveau = 1 });
                        }
                        else
                        {
                            errors.Add($"Row {row}: Prerequisite '{prereqName}' for skill '{name}' not found — skipped.");
                        }
                    }

                    if (resolvedPrereqs.Count > 0)
                    {
                        skill.Prerequisites = resolvedPrereqs;
                    }
                }

                await _db.SaveChangesAsync();
            }

            return Ok(new SkillImportResult
            {
                Imported = imported,
                Skipped = skipped,
                Errors = errors,
            });
        }
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
    public IList<CompetenceCentreProfile> Profiles { get; set; } = [];
}

public class SkillDetailResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public string? Description { get; set; }
    public int LevelCount { get; set; }
    public IList<string> LevelDescriptors { get; set; } = [];
    public IList<SkillPrerequisite> Prerequisites { get; set; } = [];

    /// <summary>
    /// Non-blocking warnings for unmet prerequisites (FR8).
    /// Skill is NEVER locked — these are informational only.
    /// </summary>
    public IList<PrerequisiteWarning> PrerequisiteWarnings { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public IList<CompetenceCentreProfile> Profiles { get; set; } = [];
}

public class PrerequisiteWarning
{
    public int SkillId { get; set; }
    public required string SkillName { get; set; }
    public int RequiredNiveau { get; set; }
    public required string WarningText { get; set; }
}

public class SkillImportResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public IList<string> Errors { get; set; } = [];
}
