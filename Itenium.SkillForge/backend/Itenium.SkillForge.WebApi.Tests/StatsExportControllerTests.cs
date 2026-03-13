using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Unit tests for the CSV export and monthly stats endpoints in StatsController.
/// </summary>
[TestFixture]
public class StatsExportControllerTests : DatabaseTestBase
{
    private StatsController _sut = null!;
    private ISkillForgeUser _user = null!;

    private const string LearnerId = "learner-user-id";

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _user.UserId.Returns(LearnerId);
        _user.IsBackOffice.Returns(true);
        _sut = new StatsController(Db, _user);
    }

    private async Task<CourseEntity> CreateCourse(string name = "Test Course")
    {
        var course = new CourseEntity { Name = name };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        return course;
    }

    [Test]
    public async Task ExportUsageCsv_WhenNotBackOffice_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);

        var result = await _sut.ExportUsageCsv();

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task ExportUsageCsv_AsBackOffice_ReturnsFileResult()
    {
        var course = await CreateCourse("My Course");
        Db.Enrollments.Add(new EnrollmentEntity
        {
            LearnerId = LearnerId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        });
        await Db.SaveChangesAsync();

        var result = await _sut.ExportUsageCsv();

        Assert.That(result, Is.TypeOf<FileContentResult>());
        var file = (FileContentResult)result;
        Assert.That(file.ContentType, Is.EqualTo("text/csv"));
        Assert.That(file.FileDownloadName, Does.Contain("usage"));
    }

    [Test]
    public async Task ExportCompletionCsv_AsBackOffice_ReturnsFileResult()
    {
        var course = await CreateCourse("Course");
        Db.Enrollments.Add(new EnrollmentEntity
        {
            LearnerId = LearnerId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        });
        await Db.SaveChangesAsync();

        var result = await _sut.ExportCompletionCsv();

        Assert.That(result, Is.TypeOf<FileContentResult>());
        var file = (FileContentResult)result;
        Assert.That(file.ContentType, Is.EqualTo("text/csv"));
        Assert.That(file.FileDownloadName, Does.Contain("completion"));
    }

    [Test]
    public async Task GetMonthlyStats_AsBackOffice_Returns12Months()
    {
        var result = await _sut.GetMonthlyStats();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var items = ok!.Value as IList<MonthlyStatsItem>;
        Assert.That(items, Is.Not.Null);
        Assert.That(items!.Count, Is.EqualTo(12));
    }

    [Test]
    public async Task GetMonthlyStats_CountsEnrollmentsAndCompletionsCorrectly()
    {
        var course = await CreateCourse();
        var now = DateTime.UtcNow;

        Db.Enrollments.AddRange(
            new EnrollmentEntity { LearnerId = LearnerId, CourseId = course.Id, EnrolledAt = now, CompletedAt = now });
        await Db.SaveChangesAsync();

        var result = await _sut.GetMonthlyStats();

        var ok = result.Result as OkObjectResult;
        var items = ok!.Value as IList<MonthlyStatsItem>;

        // The current month should have at least 1 enrollment
        var currentMonth = items!.FirstOrDefault(m => m.Year == now.Year && m.Month == now.Month);
        Assert.That(currentMonth, Is.Not.Null);
        Assert.That(currentMonth!.Enrollments, Is.GreaterThanOrEqualTo(1));
        Assert.That(currentMonth.Completions, Is.GreaterThanOrEqualTo(1));
    }
}
