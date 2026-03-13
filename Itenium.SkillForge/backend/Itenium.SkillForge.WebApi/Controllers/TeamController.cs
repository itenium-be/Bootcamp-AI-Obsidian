using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Provides access to team information in the SkillForge LMS.
/// BackOffice sees all teams; managers and learners see only their own teams.
/// CRUD operations and member management are restricted to BackOffice users.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeamController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;
    private readonly UserManager<ForgeUser> _userManager;

    /// <summary>
    /// Initializes a new instance of <see cref="TeamController"/>.
    /// </summary>
    /// <param name="db">The application database context.</param>
    /// <param name="user">The current authenticated user abstraction.</param>
    /// <param name="userManager">The ASP.NET Identity user manager.</param>
    public TeamController(AppDbContext db, ISkillForgeUser user, UserManager<ForgeUser> userManager)
    {
        _db = db;
        _user = user;
        _userManager = userManager;
    }

    /// <summary>
    /// Get the teams the current user has access to.
    /// </summary>
    /// <returns>
    /// All teams for BackOffice users; for managers and learners, only teams they are a member of.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TeamEntity>>> GetUserTeams()
    {
        if (_user.IsBackOffice)
        {
            return await _db.Teams.ToListAsync();
        }

        return await _db.Teams
            .Where(t => _user.Teams.Contains(t.Id))
            .ToListAsync();
    }

    /// <summary>
    /// Get a specific team by ID (BackOffice only).
    /// </summary>
    /// <param name="id">The unique identifier of the team.</param>
    /// <returns>The team with the specified ID.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamEntity>> GetTeam(int id)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var team = await _db.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        return Ok(team);
    }

    /// <summary>
    /// Create a new team (BackOffice only).
    /// </summary>
    /// <param name="request">The team creation request containing name and optional description.</param>
    /// <returns>The newly created team.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TeamEntity>> CreateTeam([FromBody] CreateTeamRequest request)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var team = new TeamEntity
        {
            Name = request.Name,
            Description = request.Description
        };

        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team);
    }

    /// <summary>
    /// Update an existing team's name and/or description (BackOffice only).
    /// </summary>
    /// <param name="id">The unique identifier of the team to update.</param>
    /// <param name="request">The update request containing the new name and optional description.</param>
    /// <returns>The updated team.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamEntity>> UpdateTeam(int id, [FromBody] UpdateTeamRequest request)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var team = await _db.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        team.Name = request.Name;
        team.Description = request.Description;

        await _db.SaveChangesAsync();

        return Ok(team);
    }

    /// <summary>
    /// Delete a team (BackOffice only).
    /// </summary>
    /// <param name="id">The unique identifier of the team to delete.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTeam(int id)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var team = await _db.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        _db.Teams.Remove(team);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// List all members of a team (BackOffice only).
    /// Membership is determined by the "team" claim on the user's Identity record.
    /// </summary>
    /// <param name="id">The unique identifier of the team.</param>
    /// <returns>A list of users who are members of the specified team.</returns>
    [HttpGet("{id:int}/members")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<UserResponse>>> GetTeamMembers(int id)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var team = await _db.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        var teamIdStr = id.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var userIds = await _db.Set<IdentityUserClaim<string>>()
            .Where(c => c.ClaimType == "team" && c.ClaimValue == teamIdStr)
            .Select(c => c.UserId)
            .Distinct()
            .ToListAsync();

        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();

        var responses = new List<UserResponse>();
        foreach (var u in users)
        {
            var roleIds = await _db.Set<IdentityUserRole<string>>()
                .Where(ur => ur.UserId == u.Id)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            var roles = await _db.Roles
                .Where(r => roleIds.Contains(r.Id))
                .Select(r => r.Name!)
                .ToListAsync();

            responses.Add(new UserResponse(u.Id, u.UserName, u.Email, u.FirstName, u.LastName, roles));
        }

        return Ok(responses);
    }

    /// <summary>
    /// Add a user to a team by assigning the "team" claim to their Identity record (BackOffice only).
    /// </summary>
    /// <param name="id">The unique identifier of the team.</param>
    /// <param name="request">The request body containing the user ID to add.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("{id:int}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddTeamMember(int id, [FromBody] AddTeamMemberRequest request)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var team = await _db.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound("Team not found.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var teamIdStr = id.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Check if user already has this team claim
        var existingClaims = await _userManager.GetClaimsAsync(user);
        var alreadyMember = existingClaims.Any(c =>
            c.Type == "team" &&
            c.Value.Equals(teamIdStr, StringComparison.OrdinalIgnoreCase));

        if (alreadyMember)
        {
            return BadRequest("User is already a member of this team.");
        }

        var result = await _userManager.AddClaimAsync(user, new Claim("team", teamIdStr));
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        return NoContent();
    }

    /// <summary>
    /// Remove a user from a team by removing their "team" claim (BackOffice only).
    /// </summary>
    /// <param name="id">The unique identifier of the team.</param>
    /// <param name="userId">The Identity user ID of the user to remove from the team.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:int}/members/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveTeamMember(int id, string userId)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var team = await _db.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound("Team not found.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var teamIdStr = id.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var existingClaims = await _userManager.GetClaimsAsync(user);
        var teamClaim = existingClaims.FirstOrDefault(c =>
            c.Type == "team" &&
            c.Value.Equals(teamIdStr, StringComparison.OrdinalIgnoreCase));

        if (teamClaim == null)
        {
            return NotFound("User is not a member of this team.");
        }

        var result = await _userManager.RemoveClaimAsync(user, teamClaim);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        return NoContent();
    }
}
