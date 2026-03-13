using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "backoffice")]
public class UserController(UserManager<ForgeUser> userManager) : ControllerBase
{
    /// <summary>
    /// Create a new user account with role and team assignment.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = new ForgeUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await userManager.AddToRoleAsync(user, request.Role);

        foreach (var teamId in request.TeamIds)
            await userManager.AddClaimAsync(user, new Claim("team", teamId.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        return Created($"/api/user/{user.Id}", new { user.Id, user.Email, user.FirstName, user.LastName });
    }
}
