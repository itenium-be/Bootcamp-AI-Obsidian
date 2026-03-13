using Itenium.SkillForge.Data;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public StatsController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get dashboard stats. Learner sees own scope; backoffice/manager sees global stats.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<StatsResponse>> GetStats()
    {
        int totalCourses = await _db.Courses.CountAsync();
        int totalLearners;
        int totalEnrollments;
        int totalCertificates;
        int completedEnrollments;

        if (_user.IsBackOffice)
        {
            totalLearners = await _db.Users.CountAsync();
            totalEnrollments = await _db.Enrollments.CountAsync();
            totalCertificates = await _db.Certificates.CountAsync();
            completedEnrollments = await _db.Enrollments.CountAsync(e => e.CompletedAt != null);
        }
        else
        {
            totalLearners = 1;
            totalEnrollments = await _db.Enrollments.CountAsync(e => e.LearnerId == _user.UserId);
            totalCertificates = await _db.Certificates.CountAsync(c => c.LearnerId == _user.UserId);
            completedEnrollments = await _db.Enrollments.CountAsync(e => e.LearnerId == _user.UserId && e.CompletedAt != null);
        }

        double completionRate = totalEnrollments > 0
            ? Math.Round((double)completedEnrollments / totalEnrollments * 100, 2)
            : 0.0;

        return Ok(new StatsResponse(
            TotalCourses: totalCourses,
            TotalLearners: totalLearners,
            TotalEnrollments: totalEnrollments,
            TotalCertificates: totalCertificates,
            CompletionRate: completionRate));
    }
}
