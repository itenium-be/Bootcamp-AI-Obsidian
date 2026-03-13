using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "backoffice")]
public partial class UserController(UserManager<ForgeUser> userManager, ILogger<UserController> logger) : ControllerBase
{
    private static readonly HashSet<string> AllowedRoles = ["learner", "manager", "backoffice"];

    /// <summary>
    /// Get all active (non-archived) user accounts.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = userManager.Users.ToList();
        var result = new List<object>();
        foreach (var u in users)
        {
            var claims = await userManager.GetClaimsAsync(u);
            if (claims.Any(c => c.Type == "archived" && c.Value == "true"))
                continue;

            var roles = await userManager.GetRolesAsync(u);
            result.Add(new { u.Id, u.Email, u.FirstName, u.LastName, Roles = roles });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all archived user accounts.
    /// </summary>
    [HttpGet("archived")]
    public async Task<IActionResult> GetArchivedUsers()
    {
        var users = userManager.Users.ToList();
        var result = new List<object>();
        foreach (var u in users)
        {
            var claims = await userManager.GetClaimsAsync(u);
            if (!claims.Any(c => c.Type == "archived" && c.Value == "true"))
                continue;

            var roles = await userManager.GetRolesAsync(u);
            result.Add(new { u.Id, u.Email, u.FirstName, u.LastName, Roles = roles });
        }

        return Ok(result);
    }

    /// <summary>
    /// Archive a user account (soft-delete: disables login, preserves history).
    /// </summary>
    [HttpPatch("{id}/archive")]
    public async Task<IActionResult> ArchiveUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        await userManager.AddClaimAsync(user, new Claim("archived", "true"));

        LogUserArchived(logger, user.Email ?? id);
        return Ok();
    }

    /// <summary>
    /// Restore an archived user account.
    /// </summary>
    [HttpPatch("{id}/restore")]
    public async Task<IActionResult> RestoreUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        await userManager.SetLockoutEnabledAsync(user, false);
        await userManager.SetLockoutEndDateAsync(user, null);

        var claims = await userManager.GetClaimsAsync(user);
        var archivedClaim = claims.FirstOrDefault(c => c.Type == "archived");
        if (archivedClaim != null)
            await userManager.RemoveClaimAsync(user, archivedClaim);

        LogUserRestored(logger, user.Email ?? id);
        return Ok();
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Archived user {Email}")]
    private static partial void LogUserArchived(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Restored user {Email}")]
    private static partial void LogUserRestored(ILogger logger, string email);
}
