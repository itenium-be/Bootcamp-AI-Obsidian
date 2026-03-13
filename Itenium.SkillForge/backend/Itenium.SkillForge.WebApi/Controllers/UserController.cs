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
    private static readonly HashSet<string> AllowedRoles = ["learner", "manager", "backoffice"];

    /// <summary>
    /// Get all user accounts.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = userManager.Users.ToList();
        var result = new List<object>();
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            result.Add(new { u.Id, u.Email, u.FirstName, u.LastName, Roles = roles });
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new user account with role and team assignment.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!AllowedRoles.Contains(request.Role))
            return BadRequest($"Role '{request.Role}' is not valid. Allowed: {string.Join(", ", AllowedRoles)}");

        var user = new ForgeUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors);

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
            return StatusCode(500, roleResult.Errors);

        foreach (var teamId in request.TeamIds)
        {
            var claimResult = await userManager.AddClaimAsync(user, new Claim("team", teamId.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            if (!claimResult.Succeeded)
                return StatusCode(500, claimResult.Errors);
        }

        return Created($"/api/user/{user.Id}", new { user.Id, user.Email, user.FirstName, user.LastName });
    }
}
