using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public EnrollmentController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get enrollments. Learner sees own enrollments; backoffice sees all.
    /// </summary>
    [HttpGet]
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
    [HttpPost("{courseId:int}")]
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
    [HttpDelete("{courseId:int}")]
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
