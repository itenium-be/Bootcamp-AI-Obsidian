using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

public record AssignProfileRequest(string UserId, int TeamId);

[ApiController]
[Route("api/consultant-profile")]
[Authorize]
public class ConsultantProfileController : ControllerBase
{
    private readonly AppDbContext _db;

    public ConsultantProfileController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Assign a consultant to a competence centre profile (team).
    /// If already assigned, updates the assignment.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ConsultantProfileEntity>> AssignProfile([FromBody] AssignProfileRequest request)
    {
        var existing = await _db.ConsultantProfiles
            .FirstOrDefaultAsync(p => p.UserId == request.UserId);

        if (existing == null)
        {
            var profile = new ConsultantProfileEntity
            {
                UserId = request.UserId,
                TeamId = request.TeamId,
                AssignedAt = DateTime.UtcNow,
            };
            _db.ConsultantProfiles.Add(profile);
            await _db.SaveChangesAsync();
            return Ok(profile);
        }
        else
        {
            existing.TeamId = request.TeamId;
            existing.AssignedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }
    }

    /// <summary>
    /// Get the current profile assignment for a consultant.
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<ConsultantProfileEntity>> GetProfile(string userId)
    {
        var profile = await _db.ConsultantProfiles
            .Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return NotFound();
        }

        return Ok(profile);
    }
}
