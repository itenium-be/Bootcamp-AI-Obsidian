using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Manages the assignment of teams to courses in the SkillForge LMS.
/// Allows BackOffice and managers to control which teams have access to which courses.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CourseTeamController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    /// <summary>
    /// Initializes a new instance of <see cref="CourseTeamController"/>.
    /// </summary>
    /// <param name="db">The application database context.</param>
    /// <param name="user">The current authenticated user abstraction.</param>
    public CourseTeamController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get all teams assigned to a specific course.
    /// </summary>
    /// <param name="courseId">The unique identifier of the course.</param>
    /// <returns>A list of teams assigned to the specified course.</returns>
    /// <remarks>Only accessible by BackOffice users and managers.</remarks>
    [HttpGet("{courseId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TeamEntity>>> GetTeamsForCourse(int courseId)
    {
        if (!_user.IsBackOffice && _user.Teams.Count == 0)
        {
            return Forbid();
        }

        var course = await _db.Courses.FindAsync(courseId);
        if (course == null)
        {
            return NotFound();
        }

        var teams = await _db.CourseTeams
            .Where(ct => ct.CourseId == courseId)
            .Include(ct => ct.Team)
            .Select(ct => ct.Team!)
            .ToListAsync();

        return Ok(teams);
    }

    /// <summary>
    /// Assign a team to a course.
    /// </summary>
    /// <param name="courseId">The unique identifier of the course.</param>
    /// <param name="teamId">The unique identifier of the team to assign.</param>
    /// <returns>The newly created course-team assignment record.</returns>
    /// <remarks>
    /// Only accessible by BackOffice users.
    /// Returns 409 if the team is already assigned to the course.
    /// </remarks>
    [HttpPost("{courseId:int}/teams/{teamId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseTeamEntity>> AssignTeam(int courseId, int teamId)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var course = await _db.Courses.FindAsync(courseId);
        if (course == null)
        {
            return NotFound("Course not found.");
        }

        var team = await _db.Teams.FindAsync(teamId);
        if (team == null)
        {
            return NotFound("Team not found.");
        }

        var existing = await _db.CourseTeams
            .FirstOrDefaultAsync(ct => ct.CourseId == courseId && ct.TeamId == teamId);

        if (existing != null)
        {
            return Conflict("Team is already assigned to this course.");
        }

        var assignment = new CourseTeamEntity
        {
            CourseId = courseId,
            TeamId = teamId,
            AssignedAt = DateTime.UtcNow
        };

        _db.CourseTeams.Add(assignment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTeamsForCourse), new { courseId }, assignment);
    }

    /// <summary>
    /// Remove a team assignment from a course.
    /// </summary>
    /// <param name="courseId">The unique identifier of the course.</param>
    /// <param name="teamId">The unique identifier of the team to remove.</param>
    /// <returns>No content on success.</returns>
    /// <remarks>Only accessible by BackOffice users.</remarks>
    [HttpDelete("{courseId:int}/teams/{teamId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveTeam(int courseId, int teamId)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var assignment = await _db.CourseTeams
            .FirstOrDefaultAsync(ct => ct.CourseId == courseId && ct.TeamId == teamId);

        if (assignment == null)
        {
            return NotFound();
        }

        _db.CourseTeams.Remove(assignment);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
