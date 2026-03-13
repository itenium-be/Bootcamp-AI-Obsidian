using System.Globalization;
using System.Text;
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
            CoursesByCategory: coursesByCategory.ToDictionary(x => x.Category, x => x.Count, StringComparer.OrdinalIgnoreCase),
            AverageProgress: averageProgress));
    }

    /// <summary>
    /// Export all enrollments as a CSV file (BackOffice only).
    /// Each row contains: learner ID, course name, enrolled date, completion date.
    /// </summary>
    /// <returns>A CSV file with usage data.</returns>
    [HttpGet("export/usage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportUsageCsv()
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var enrollments = await _db.Enrollments
            .Include(e => e.Course)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("LearnerId,CourseName,EnrolledAt,CompletedAt");

        foreach (var e in enrollments)
        {
            var courseName = e.Course?.Name ?? string.Empty;
            var completedAt = e.CompletedAt.HasValue
                ? e.CompletedAt.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty;
            sb.AppendLine(FormattableString.Invariant(
                $"{EscapeCsv(e.LearnerId)},{EscapeCsv(courseName)},{e.EnrolledAt:yyyy-MM-dd},{completedAt}"));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"usage-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Export completion rates per course as a CSV file (BackOffice only).
    /// Each row contains: course name, total enrollments, completed enrollments, completion rate (%).
    /// </summary>
    /// <returns>A CSV file with completion rate data per course.</returns>
    [HttpGet("export/completion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportCompletionCsv()
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var courseStats = await _db.Enrollments
            .Include(e => e.Course)
            .GroupBy(e => new { e.CourseId, CourseName = e.Course!.Name })
            .Select(g => new
            {
                g.Key.CourseName,
                Total = g.Count(),
                Completed = g.Count(e => e.CompletedAt != null)
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("CourseName,TotalEnrollments,CompletedEnrollments,CompletionRate");

        foreach (var s in courseStats)
        {
            double rate = s.Total > 0 ? Math.Round((double)s.Completed / s.Total * 100, 2) : 0.0;
            sb.AppendLine(FormattableString.Invariant(
                $"{EscapeCsv(s.CourseName)},{s.Total},{s.Completed},{rate}"));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"completion-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Get monthly enrollment and completion counts for the last 12 months.
    /// Intended to replace hardcoded chart data in the frontend dashboard.
    /// </summary>
    /// <returns>An array of 12 monthly stats items ordered from oldest to newest.</returns>
    [HttpGet("monthly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IList<MonthlyStatsItem>>> GetMonthlyStats()
    {
        var cutoff = DateTime.UtcNow.AddMonths(-11);
        var startDate = new DateTime(cutoff.Year, cutoff.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var enrollments = await _db.Enrollments
            .Where(e => e.EnrolledAt >= startDate)
            .ToListAsync();

        var result = new List<MonthlyStatsItem>();
        for (var i = 0; i < 12; i++)
        {
            var month = startDate.AddMonths(i);
            var year = month.Year;
            var monthNum = month.Month;

            var enrolled = enrollments.Count(e => e.EnrolledAt.Year == year && e.EnrolledAt.Month == monthNum);
            var completed = enrollments.Count(e =>
                e.CompletedAt.HasValue &&
                e.CompletedAt.Value.Year == year &&
                e.CompletedAt.Value.Month == monthNum);

            result.Add(new MonthlyStatsItem(year, monthNum, enrolled, completed));
        }

        return Ok(result);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',', StringComparison.Ordinal) ||
            value.Contains('"', StringComparison.Ordinal) ||
            value.Contains('\n', StringComparison.Ordinal))
        {
            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return value;
    }
}
