using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.WebApi.Controllers;

public record CreateUserRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, MinLength(8)] string Password,
    [Required] string Role,
    ICollection<int> TeamIds);
