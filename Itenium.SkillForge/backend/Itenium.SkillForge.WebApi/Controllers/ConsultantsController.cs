using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/consultants")]
[Authorize]
public class ConsultantsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ConsultantsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get a consultant's assigned competence centre profile.
    /// GET /api/consultants/{id}/profile
    /// </summary>
    [HttpGet("{id}/profile")]
    public async Task<ActionResult<ConsultantProfileResponse>> GetConsultantProfile(string id)
    {
        var profile = await _db.ConsultantProfiles
            .FirstOrDefaultAsync(cp => cp.UserId == id);

        if (profile == null)
        {
            return NotFound();
        }

        return Ok(new ConsultantProfileResponse
        {
            UserId = profile.UserId,
            Profile = profile.Profile,
            AssignedAt = profile.AssignedAt,
            AssignedBy = profile.AssignedBy,
        });
    }

    /// <summary>
    /// Assign (or re-assign) a consultant to a competence centre profile.
    /// PUT /api/consultants/{id}/profile
    /// Requires manager or backoffice role.
    /// Journey 4: Coach assigns Sander to Java profile before his first login.
    /// </summary>
    [HttpPut("{id}/profile")]
    public async Task<IActionResult> AssignProfile(string id, [FromBody] AssignProfileRequest request)
    {
        var existing = await _db.ConsultantProfiles
            .FirstOrDefaultAsync(cp => cp.UserId == id);

        if (existing == null)
        {
            _db.ConsultantProfiles.Add(new ConsultantProfileEntity
            {
                UserId = id,
                Profile = request.Profile,
                AssignedBy = request.AssignedBy,
                AssignedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.Profile = request.Profile;
            existing.AssignedBy = request.AssignedBy;
            existing.AssignedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }
}

// ── Request / Response models ────────────────────────────────────────────────

public record AssignProfileRequest(CompetenceCentreProfile Profile, string? AssignedBy);

public class ConsultantProfileResponse
{
    public required string UserId { get; set; }
    public CompetenceCentreProfile Profile { get; set; }
    public DateTime AssignedAt { get; set; }
    public string? AssignedBy { get; set; }
}
