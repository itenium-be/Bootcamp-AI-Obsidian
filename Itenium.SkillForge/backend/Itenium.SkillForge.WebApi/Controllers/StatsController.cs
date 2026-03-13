using Itenium.SkillForge.Data;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Provides dashboard statistics for the SkillForge LMS.
/// BackOffice and managers see global stats; learners see stats scoped to their own activity.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    /// <summary>
    /// Initializes a new instance of <see cref="StatsController"/>.
    /// </summary>
    /// <param name="db">The application database context.</param>
    /// <param name="user">The current authenticated user abstraction.</param>
    public StatsController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get dashboard stats. Learner sees own scope; backoffice/manager sees global stats.
    /// </summary>
    /// <returns>
    /// An aggregate stats response including total courses, learners, enrollments,
    /// certificates, completion rate, active learners, courses by category, and average progress.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<StatsResponse>> GetStats()
    {
        int totalCourses = await _db.Courses.CountAsync();
        int totalLearners;
        int totalEnrollments;
        int totalCertificates;
        int completedEnrollments;
        int activeLearners;
        double averageProgress;

        if (_user.IsBackOffice)
        {
            totalLearners = await _db.Users.CountAsync();
            totalEnrollments = await _db.Enrollments.CountAsync();
            totalCertificates = await _db.Certificates.CountAsync();
            completedEnrollments = await _db.Enrollments.CountAsync(e => e.CompletedAt != null);

            // Active learners: enrolled in at least one course but not yet completed all
            var learnerEnrollmentCounts = await _db.Enrollments
                .GroupBy(e => e.LearnerId)
                .Select(g => new
                {
                    LearnerId = g.Key,
                    Total = g.Count(),
                    Completed = g.Count(e => e.CompletedAt != null)
                })
                .ToListAsync();

            activeLearners = learnerEnrollmentCounts
                .Count(l => l.Total > l.Completed);

            averageProgress = await _db.Progresses.AnyAsync()
                ? Math.Round(await _db.Progresses.AverageAsync(p => (double)p.PercentageComplete), 2)
                : 0.0;
        }
        else
        {
            totalLearners = 1;
            totalEnrollments = await _db.Enrollments.CountAsync(e => e.LearnerId == _user.UserId);
            totalCertificates = await _db.Certificates.CountAsync(c => c.LearnerId == _user.UserId);
            completedEnrollments = await _db.Enrollments.CountAsync(e => e.LearnerId == _user.UserId && e.CompletedAt != null);
            activeLearners = totalEnrollments > completedEnrollments ? 1 : 0;

            averageProgress = await _db.Progresses.AnyAsync(p => p.LearnerId == _user.UserId)
                ? Math.Round(await _db.Progresses
                    .Where(p => p.LearnerId == _user.UserId)
                    .AverageAsync(p => (double)p.PercentageComplete), 2)
                : 0.0;
        }

        double completionRate = totalEnrollments > 0
            ? Math.Round((double)completedEnrollments / totalEnrollments * 100, 2)
            : 0.0;

        var coursesByCategory = await _db.Courses
            .GroupBy(c => c.Category ?? "Uncategorized")
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new StatsResponse(
            TotalCourses: totalCourses,
            TotalLearners: totalLearners,
            TotalEnrollments: totalEnrollments,
            TotalCertificates: totalCertificates,
            CompletionRate: completionRate,
            ActiveLearners: activeLearners,
            CoursesByCategory: coursesByCategory.ToDictionary(x => x.Category, x => x.Count),
            AverageProgress: averageProgress));
    }
}
