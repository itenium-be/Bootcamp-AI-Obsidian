using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Provides access to team information in the SkillForge LMS.
/// BackOffice sees all teams; managers and learners see only their own teams.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeamController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    /// <summary>
    /// Initializes a new instance of <see cref="TeamController"/>.
    /// </summary>
    /// <param name="db">The application database context.</param>
    /// <param name="user">The current authenticated user abstraction.</param>
    public TeamController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
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
}
