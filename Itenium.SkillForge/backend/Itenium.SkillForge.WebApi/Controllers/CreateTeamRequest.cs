using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Request body for creating a new team.
/// </summary>
/// <param name="Name">The name of the team. Required, max 200 characters.</param>
/// <param name="Description">An optional description of the team. Max 1000 characters.</param>
public record CreateTeamRequest(
    [Required][MaxLength(200)] string Name,
    [MaxLength(1000)] string? Description);
