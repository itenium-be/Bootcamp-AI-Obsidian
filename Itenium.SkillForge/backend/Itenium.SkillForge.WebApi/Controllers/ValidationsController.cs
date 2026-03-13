using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/validations")]
[Authorize]
public class ValidationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public ValidationsController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get validations for a consultant.
    /// GET /api/validations?consultantId={id}
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ValidationEntity>>> GetValidations([FromQuery] string? consultantId)
    {
        IQueryable<ValidationEntity> query = _db.Validations
            .Include(v => v.Skill);

        if (!string.IsNullOrEmpty(consultantId))
        {
            query = query.Where(v => v.ConsultantId == consultantId);
        }

        var validations = await query
            .OrderByDescending(v => v.ValidatedAt)
            .ToListAsync();

        return Ok(validations);
    }

    /// <summary>
    /// Record a skill validation.
    /// POST /api/validations
    /// FR4: Restricted to manager role only. Returns 403 for learner and backoffice.
    /// FR36: ValidatedBy and ValidatedAt are set server-side and are immutable.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ValidationEntity>> CreateValidation([FromBody] CreateValidationRequest request)
    {
        // FR4: Only manager (coach) role can write skill validations.
        // backoffice and learner roles receive 403 Forbidden.
        if (!_user.IsManager)
        {
            return Forbid();
        }

        var validation = new ValidationEntity
        {
            SkillId = request.SkillId,
            ConsultantId = request.ConsultantId,
            // FR36: ValidatedBy is always the currently authenticated coach — never from the request body.
            ValidatedBy = _user.Id ?? string.Empty,
            // FR36: ValidatedAt is always server-side UTC — never from the request body.
            ValidatedAt = DateTime.UtcNow,
            FromNiveau = request.FromNiveau,
            ToNiveau = request.ToNiveau,
            Notes = request.Notes,
            SessionId = request.SessionId,
        };

        _db.Validations.Add(validation);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetValidations), new { consultantId = validation.ConsultantId }, validation);
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public record CreateValidationRequest(
    int SkillId,
    string ConsultantId,
    int FromNiveau,
    int ToNiveau,
    string? Notes,
    Guid? SessionId);
