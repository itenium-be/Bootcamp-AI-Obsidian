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
public class ProgressController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public ProgressController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get current user's progress on all enrolled courses.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ProgressEntity>>> GetAllProgress()
    {
        var progress = await _db.Progresses
            .Where(p => p.LearnerId == _user.UserId)
            .ToListAsync();

        return Ok(progress);
    }

    /// <summary>
    /// Get progress for a specific course.
    /// </summary>
    [HttpGet("{courseId:int}")]
    public async Task<ActionResult<ProgressEntity>> GetProgress(int courseId)
    {
        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.LearnerId == _user.UserId && e.CourseId == courseId);

        if (enrollment == null)
        {
            return NotFound();
        }

        var progress = await _db.Progresses
            .FirstOrDefaultAsync(p => p.LearnerId == _user.UserId && p.CourseId == courseId);

        if (progress == null)
        {
            return NotFound();
        }

        return Ok(progress);
    }

    /// <summary>
    /// Update progress for a course. Auto-issues a certificate when progress reaches 100%.
    /// </summary>
    [HttpPut("{courseId:int}")]
    public async Task<ActionResult<ProgressEntity>> UpdateProgress(int courseId, [FromBody] UpdateProgressRequest request)
    {
        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.LearnerId == _user.UserId && e.CourseId == courseId);

        if (enrollment == null)
        {
            return NotFound();
        }

        var progress = await _db.Progresses
            .FirstOrDefaultAsync(p => p.LearnerId == _user.UserId && p.CourseId == courseId);

        if (progress == null)
        {
            progress = new ProgressEntity
            {
                LearnerId = _user.UserId!,
                CourseId = courseId,
                PercentageComplete = request.PercentageComplete,
                LastUpdated = DateTime.UtcNow,
                Notes = request.Notes
            };
            _db.Progresses.Add(progress);
        }
        else
        {
            progress.PercentageComplete = request.PercentageComplete;
            progress.LastUpdated = DateTime.UtcNow;
            progress.Notes = request.Notes;
        }

        if (request.PercentageComplete >= 100)
        {
            // Mark enrollment as complete
            enrollment.CompletedAt ??= DateTime.UtcNow;

            // Auto-issue certificate if not already issued
            var existingCert = await _db.Certificates
                .FirstOrDefaultAsync(c => c.LearnerId == _user.UserId && c.CourseId == courseId);

            if (existingCert == null)
            {
                var course = await _db.Courses.FindAsync(courseId);
                var certNumber = await GenerateCertificateNumber();

                _db.Certificates.Add(new CertificateEntity
                {
                    LearnerId = _user.UserId!,
                    LearnerName = _user.UserId!,
                    CourseId = courseId,
                    CourseName = course!.Name,
                    IssuedAt = DateTime.UtcNow,
                    CertificateNumber = certNumber
                });
            }
        }

        await _db.SaveChangesAsync();

        return Ok(progress);
    }

    private async Task<string> GenerateCertificateNumber()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Certificates.CountAsync() + 1;
        return $"CERT-{year}-{count:D6}";
    }
}
