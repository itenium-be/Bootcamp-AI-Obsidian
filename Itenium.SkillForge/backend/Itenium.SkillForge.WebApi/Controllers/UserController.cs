using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Data;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public UserController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// List all users with their roles (BackOffice only).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetUsers()
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var users = await _db.Users.ToListAsync();

        var responses = new List<UserResponse>();
        foreach (var u in users)
        {
            var roles = await GetUserRoles(u.Id);
            responses.Add(new UserResponse(u.Id, u.UserName, u.Email, u.FirstName, u.LastName, roles));
        }

        return Ok(responses);
    }

    /// <summary>
    /// Get a specific user by ID (BackOffice only).
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetUser(string id)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var u = await _db.Users.FindAsync(id);
        if (u == null)
        {
            return NotFound();
        }

        var roles = await GetUserRoles(u.Id);
        return Ok(new UserResponse(u.Id, u.UserName, u.Email, u.FirstName, u.LastName, roles));
    }

    private async Task<IList<string>> GetUserRoles(string userId)
    {
        var roleIds = await _db.Set<IdentityUserRole<string>>()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var roles = await _db.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name!)
            .ToListAsync();

        return roles;
    }
}
