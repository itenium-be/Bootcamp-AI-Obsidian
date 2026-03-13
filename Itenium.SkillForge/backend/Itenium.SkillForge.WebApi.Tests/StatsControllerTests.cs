using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class StatsControllerTests : DatabaseTestBase
{
    private StatsController _sut = null!;
    private ISkillForgeUser _user = null!;

    private const string LearnerId = "learner-user-id";
    private const string OtherLearnerId = "other-learner-id";

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _user.UserId.Returns(LearnerId);
        _user.IsBackOffice.Returns(false);
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
    public async Task GetStats_AsLearner_ReturnsScopedStats()
    {
        var course1 = await CreateCourse("Course 1");
        var course2 = await CreateCourse("Course 2");

        // My enrollments
        Db.Enrollments.AddRange(
            new EnrollmentEntity { LearnerId = LearnerId, CourseId = course1.Id },
            new EnrollmentEntity { LearnerId = LearnerId, CourseId = course2.Id });

        // Other learner's enrollment
        Db.Enrollments.Add(new EnrollmentEntity { LearnerId = OtherLearnerId, CourseId = course1.Id });

        // My certificate
        Db.Certificates.Add(new CertificateEntity
        {
            LearnerId = LearnerId,
            LearnerName = "Test Learner",
            CourseId = course1.Id,
            CourseName = "Course 1",
            IssuedAt = DateTime.UtcNow,
            CertificateNumber = "CERT-2026-000001"
        });

        await Db.SaveChangesAsync();

        var result = await _sut.GetStats();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var stats = ok!.Value as StatsResponse;
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats!.TotalEnrollments, Is.EqualTo(2)); // my enrollments
        Assert.That(stats.TotalCertificates, Is.EqualTo(1)); // my certs
        Assert.That(stats.TotalCourses, Is.EqualTo(2));
    }

    [Test]
    public async Task GetStats_AsBackOffice_ReturnsGlobalStats()
    {
        _user.IsBackOffice.Returns(true);

        var course1 = await CreateCourse("Course 1");
        var course2 = await CreateCourse("Course 2");

        Db.Enrollments.AddRange(
            new EnrollmentEntity { LearnerId = LearnerId, CourseId = course1.Id },
            new EnrollmentEntity { LearnerId = OtherLearnerId, CourseId = course2.Id });

        Db.Certificates.AddRange(
            new CertificateEntity { LearnerId = LearnerId, LearnerName = "A", CourseId = course1.Id, CourseName = "C1", IssuedAt = DateTime.UtcNow, CertificateNumber = "CERT-2026-000001" },
            new CertificateEntity { LearnerId = OtherLearnerId, LearnerName = "B", CourseId = course2.Id, CourseName = "C2", IssuedAt = DateTime.UtcNow, CertificateNumber = "CERT-2026-000002" });

        await Db.SaveChangesAsync();

        var result = await _sut.GetStats();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var stats = ok!.Value as StatsResponse;
        Assert.That(stats!.TotalEnrollments, Is.EqualTo(2));
        Assert.That(stats.TotalCertificates, Is.EqualTo(2));
        Assert.That(stats.TotalCourses, Is.EqualTo(2));
    }

    [Test]
    public async Task GetStats_CompletionRate_CalculatedCorrectly()
    {
        _user.IsBackOffice.Returns(true);

        var course = await CreateCourse();
        Db.Enrollments.AddRange(
            new EnrollmentEntity { LearnerId = LearnerId, CourseId = course.Id, CompletedAt = DateTime.UtcNow },
            new EnrollmentEntity { LearnerId = OtherLearnerId, CourseId = course.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.GetStats();
        var ok = result.Result as OkObjectResult;
        var stats = ok!.Value as StatsResponse;

        // 1 out of 2 completed = 50%
        Assert.That(stats!.CompletionRate, Is.EqualTo(50.0).Within(0.01));
    }
}
