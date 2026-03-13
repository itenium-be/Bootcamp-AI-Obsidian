using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Request body for adding a user to a team.
/// </summary>
/// <param name="UserId">The Identity user ID of the user to add to the team.</param>
public record AddTeamMemberRequest(
    [Required] string UserId);
