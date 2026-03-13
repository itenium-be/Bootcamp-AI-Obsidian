using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Request body for creating a new user.
/// </summary>
/// <param name="UserName">The unique username. Required.</param>
/// <param name="Email">The user's email address. Required.</param>
/// <param name="Password">The initial password for the user. Required.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="Roles">The list of roles to assign to the user (e.g. "backoffice", "manager", "learner").</param>
public record CreateUserRequest(
    [Required] string UserName,
    [Required][EmailAddress] string Email,
    [Required] string Password,
    string? FirstName,
    string? LastName,
    IList<string> Roles);
