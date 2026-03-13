using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Admin user management controller.
/// Story #15: Create user accounts with role and team assignment.
/// Story #36: Archive (soft-delete) user accounts.
/// Story #37: Restore archived user accounts.
/// Story #38: List consultants without an active coach.
/// All endpoints restricted to backoffice role (FR3/FR5/FR40/FR41/FR42/FR43).
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "backoffice")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ForgeUser> _userManager;
    private readonly AppDbContext _db;

    public UsersController(UserManager<ForgeUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    /// <summary>
    /// List all non-archived users.
    /// GET /api/users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetUsers()
    {
        var users = await _db.Users
            .Where(u => !EF.Property<bool>(u, "IsArchived"))
            .ToListAsync();

        var result = new List<UserResponse>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            var teamIds = claims
                .Where(c => c.Type == "team")
                .Select(c => int.TryParse(c.Value, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            result.Add(new UserResponse
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty,
                TeamIds = teamIds,
                IsArchived = false,
                ArchivedAt = null,
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new user account with role and team assignment.
    /// POST /api/users
    /// FR5/FR40: Admin assigns role (learner/manager/backoffice) and team membership.
    /// Journey 4: Sander's account created in ~2 minutes.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!IsValidRole(request.Role))
        {
            return BadRequest($"Invalid role. Allowed values: learner, manager, backoffice.");
        }

        var user = new ForgeUser
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = true,
            FirstName = request.FirstName,
            LastName = request.LastName,
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(createResult.Errors.Select(e => e.Description));
        }

        await _userManager.AddToRoleAsync(user, request.Role);

        foreach (var teamId in request.TeamIds)
        {
            await _userManager.AddClaimAsync(user, new Claim("team", teamId.ToString()));
        }

        return CreatedAtAction(nameof(GetUsers), new { }, new UserResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            Role = request.Role,
            TeamIds = request.TeamIds,
            IsArchived = false,
            ArchivedAt = null,
        });
    }

    /// <summary>
    /// Archive a user account (soft-delete).
    /// POST /api/users/{id}/archive
    /// FR41: Disables login, hides from active views, preserves all coaching history.
    /// No hard deletion — archive filter applied at repository layer.
    /// </summary>
    [HttpPost("{id}/archive")]
    public async Task<IActionResult> ArchiveUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (EF.Property<bool>(user, "IsArchived"))
        {
            return Conflict("User is already archived.");
        }

        // Set shadow properties
        _db.Entry(user).Property("IsArchived").CurrentValue = true;
        _db.Entry(user).Property("ArchivedAt").CurrentValue = DateTime.UtcNow;

        // Lock account to prevent login (FR41)
        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Restore an archived user account.
    /// POST /api/users/{id}/restore
    /// FR42: Restores login access with full history intact.
    /// Restricted to backoffice role only.
    /// </summary>
    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (!EF.Property<bool>(user, "IsArchived"))
        {
            return Conflict("User is not archived.");
        }

        // Clear archive shadow properties
        _db.Entry(user).Property("IsArchived").CurrentValue = false;
        _db.Entry(user).Property("ArchivedAt").CurrentValue = null;

        // Re-enable login
        await _userManager.SetLockoutEndDateAsync(user, null);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// List consultants (learner role) not currently assigned to an active coach.
    /// GET /api/users/orphaned-consultants
    /// FR43: Accessible to backoffice role only. Orphaned consultants remain visible for reassignment.
    /// </summary>
    [HttpGet("orphaned-consultants")]
    public async Task<ActionResult<List<UserResponse>>> GetOrphanedConsultants()
    {
        // Get all non-archived learners
        var allLearners = await _db.Users
            .Where(u => !EF.Property<bool>(u, "IsArchived"))
            .ToListAsync();

        var learnerIds = new List<string>();
        foreach (var u in allLearners)
        {
            if (await _userManager.IsInRoleAsync(u, "learner"))
            {
                learnerIds.Add(u.Id);
            }
        }

        // Find learners who have no active goal with a currently active coach.
        // A learner is "orphaned" if they have no ConsultantProfile assigned,
        // OR their assigned coach is archived (inactive).
        var consultantsWithActiveCoach = await _db.ConsultantProfiles
            .Where(cp => cp.AssignedBy != null)
            .Select(cp => cp.UserId)
            .ToListAsync();

        // Find coaches that are archived
        var archivedCoaches = await _db.Users
            .Where(u => EF.Property<bool>(u, "IsArchived"))
            .Select(u => u.Id)
            .ToListAsync();

        // Consultants with active goals referencing an archived coach are also orphaned
        var consultantsWithArchivedCoach = await _db.Goals
            .Where(g => archivedCoaches.Contains(g.CoachId))
            .Select(g => g.ConsultantId)
            .Distinct()
            .ToListAsync();

        var orphanedIds = learnerIds
            .Where(id => !consultantsWithActiveCoach.Contains(id) || consultantsWithArchivedCoach.Contains(id))
            .ToList();

        var orphanedUsers = allLearners.Where(u => orphanedIds.Contains(u.Id)).ToList();

        var result = new List<UserResponse>();
        foreach (var user in orphanedUsers)
        {
            result.Add(new UserResponse
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Role = "learner",
                TeamIds = [],
                IsArchived = false,
                ArchivedAt = null,
            });
        }

        return Ok(result);
    }

    private static bool IsValidRole(string role) =>
        role is "learner" or "manager" or "backoffice";
}

// ── Request / Response models ─────────────────────────────────────────────────

public record CreateUserRequest(
    string UserName,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    List<int> TeamIds);

public class UserResponse
{
    public required string Id { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Role { get; set; }
    public List<int> TeamIds { get; set; } = [];
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
}
