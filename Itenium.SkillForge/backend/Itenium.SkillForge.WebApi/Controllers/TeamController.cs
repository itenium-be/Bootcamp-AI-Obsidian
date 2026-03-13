using System.Globalization;
using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeamController(AppDbContext db, ISkillForgeUser user, UserManager<ForgeUser> userManager) : ControllerBase
{
    /// <summary>
    /// Get the teams the current user has access to.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TeamEntity>>> GetUserTeams()
    {
        if (user.IsBackOffice)
        {
            return await db.Teams.ToListAsync();
        }

        return await db.Teams
            .Where(t => user.Teams.Contains(t.Id))
            .ToListAsync();
    }

    /// <summary>
    /// Get all members of a team.
    /// </summary>
    [HttpGet("{id}/members")]
    [Authorize(Roles = "backoffice")]
    public async Task<IActionResult> GetTeamMembers(int id)
    {
        var team = await db.Teams.FindAsync(id);
        if (team == null) return NotFound();

        var allUsers = userManager.Users.ToList();
        var members = new List<object>();
        foreach (var u in allUsers)
        {
            var claims = await userManager.GetClaimsAsync(u);
            if (claims.Any(c => c.Type == "team" && c.Value == id.ToString(CultureInfo.InvariantCulture)))
                members.Add(new { u.Id, u.Email, u.FirstName, u.LastName });
        }

        return Ok(members);
    }

    /// <summary>
    /// Add a user to a team.
    /// </summary>
    [HttpPost("{id}/members")]
    [Authorize(Roles = "backoffice")]
    public async Task<IActionResult> AddTeamMember(int id, [FromBody] AddTeamMemberRequest request)
    {
        var team = await db.Teams.FindAsync(id);
        if (team == null) return NotFound();

        var u = await userManager.FindByIdAsync(request.UserId);
        if (u == null) return NotFound("User not found");

        var existingClaims = await userManager.GetClaimsAsync(u);
        if (existingClaims.Any(c => c.Type == "team" && c.Value == id.ToString(CultureInfo.InvariantCulture)))
            return Conflict("User is already a member of this team");

        var result = await userManager.AddClaimAsync(u, new Claim("team", id.ToString(CultureInfo.InvariantCulture)));
        if (!result.Succeeded) return StatusCode(500, result.Errors);

        return Ok();
    }

    /// <summary>
    /// Remove a user from a team.
    /// </summary>
    [HttpDelete("{id}/members/{userId}")]
    [Authorize(Roles = "backoffice")]
    public async Task<IActionResult> RemoveTeamMember(int id, string userId)
    {
        var u = await userManager.FindByIdAsync(userId);
        if (u == null) return NotFound();

        var claims = await userManager.GetClaimsAsync(u);
        var claim = claims.FirstOrDefault(c => c.Type == "team" && c.Value == id.ToString(CultureInfo.InvariantCulture));
        if (claim == null) return NotFound("User is not a member of this team");

        var result = await userManager.RemoveClaimAsync(u, claim);
        if (!result.Succeeded) return StatusCode(500, result.Errors);

        return Ok();
    }
}
