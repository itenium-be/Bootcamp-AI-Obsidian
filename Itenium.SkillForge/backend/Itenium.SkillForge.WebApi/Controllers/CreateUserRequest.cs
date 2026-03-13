namespace Itenium.SkillForge.WebApi.Controllers;

public record CreateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string Role,
    ICollection<int> TeamIds);
