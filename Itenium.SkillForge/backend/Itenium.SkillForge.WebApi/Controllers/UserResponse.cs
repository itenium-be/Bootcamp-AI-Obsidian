namespace Itenium.SkillForge.WebApi.Controllers;

public record UserResponse(
    string Id,
    string? UserName,
    string? Email,
    string? FirstName,
    string? LastName,
    IList<string> Roles);
