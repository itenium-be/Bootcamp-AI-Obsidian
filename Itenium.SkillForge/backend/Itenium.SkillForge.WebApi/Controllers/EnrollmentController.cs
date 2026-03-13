using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Manages course enrollments for learners.
/// Learners can enroll/unenroll from courses; BackOffice can view all enrollments.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    /// <summary>
    /// Initializes a new instance of <see cref="EnrollmentController"/>.
    /// </summary>
    /// <param name="db">The application database context.</param>
    /// <param name="user">The current authenticated user abstraction.</param>
    public EnrollmentController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get enrollments. Learner sees own enrollments; backoffice sees all.
    /// </summary>
    /// <returns>A list of enrollments scoped to the current user or all enrollments for BackOffice.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EnrollmentEntity>>> GetEnrollments()
    {
        var query = _db.Enrollments.AsQueryable();

        if (!_user.IsBackOffice)
        {
            query = query.Where(e => e.LearnerId == _user.UserId);
        }

        return Ok(await query.ToListAsync());
    }

    /// <summary>
    /// Enroll the current user in a course.
    /// </summary>
    /// <param name="courseId">The unique identifier of the course to enroll in.</param>
    /// <returns>The newly created enrollment record.</returns>
    /// <remarks>Returns 404 if the course does not exist; returns 409 if already enrolled.</remarks>
    [HttpPost("{courseId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Enroll(int courseId)
    {
        var course = await _db.Courses.FindAsync(courseId);
        if (course == null)
        {
            return NotFound();
        }

        var existing = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.LearnerId == _user.UserId && e.CourseId == courseId);

        if (existing != null)
        {
            return Conflict("Already enrolled in this course.");
        }

        var enrollment = new EnrollmentEntity
        {
            LearnerId = _user.UserId!,
            CourseId = courseId,
            EnrolledAt = DateTime.UtcNow
        };

        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEnrollments), new { courseId }, enrollment);
    }

    /// <summary>
    /// Unenroll the current user from a course.
    /// </summary>
    /// <param name="courseId">The unique identifier of the course to unenroll from.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{courseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Unenroll(int courseId)
    {
        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.LearnerId == _user.UserId && e.CourseId == courseId);

        if (enrollment == null)
        {
            return NotFound();
        }

        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
