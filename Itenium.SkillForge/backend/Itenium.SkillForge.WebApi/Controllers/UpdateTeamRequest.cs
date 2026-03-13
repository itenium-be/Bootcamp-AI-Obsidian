using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Request body for updating an existing team.
/// </summary>
/// <param name="Name">The new name of the team. Required, max 200 characters.</param>
/// <param name="Description">An optional new description of the team. Max 1000 characters.</param>
public record UpdateTeamRequest(
    [Required][MaxLength(200)] string Name,
    [MaxLength(1000)] string? Description);
