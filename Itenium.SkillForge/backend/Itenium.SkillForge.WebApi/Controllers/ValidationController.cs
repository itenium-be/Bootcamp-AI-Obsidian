using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Manages skill validations. Only the manager (coach) role may create validations (FR4).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "manager")]
public class ValidationController(AppDbContext db, ISkillForgeUser currentUser) : ControllerBase
{
    /// <summary>
    /// Record a skill validation. ValidatedBy and ValidatedAt are set server-side and are immutable (FR36).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateValidation([FromBody] CreateValidationRequest request)
    {
        var validation = new SkillValidationEntity
        {
            SkillName = request.SkillName,
            LearnerId = request.LearnerId,
            ValidatedBy = currentUser.UserId ?? string.Empty,
            ValidatedAt = DateTime.UtcNow,
            Level = request.Level,
        };

        db.SkillValidations.Add(validation);
        await db.SaveChangesAsync();

        return Created($"/api/validation/{validation.Id}", validation);
    }
}

public record CreateValidationRequest(string SkillName, string LearnerId, string Level);
