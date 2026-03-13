using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class ProgressControllerTests : DatabaseTestBase
{
    private ProgressController _sut = null!;
    private ISkillForgeUser _user = null!;

    private const string LearnerId = "learner-user-id";
    private const string LearnerName = "Test Learner";

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _user.UserId.Returns(LearnerId);
        _user.FullName.Returns(LearnerName);
        _user.IsBackOffice.Returns(false);
        _sut = new ProgressController(Db, _user);
    }

    private async Task<CourseEntity> CreateCourse(string name = "Test Course")
    {
        var course = new CourseEntity { Name = name };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        return course;
    }

    private async Task EnrollLearner(int courseId, string? learnerId = null)
    {
        Db.Enrollments.Add(new EnrollmentEntity { LearnerId = learnerId ?? LearnerId, CourseId = courseId });
        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task GetAllProgress_ReturnsCurrentUserProgress()
    {
        var course1 = await CreateCourse("Course 1");
        var course2 = await CreateCourse("Course 2");
        await EnrollLearner(course1.Id);
        await EnrollLearner(course2.Id);

        Db.Progresses.AddRange(
            new ProgressEntity { LearnerId = LearnerId, CourseId = course1.Id, PercentageComplete = 50, LastUpdated = DateTime.UtcNow },
            new ProgressEntity { LearnerId = LearnerId, CourseId = course2.Id, PercentageComplete = 25, LastUpdated = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await _sut.GetAllProgress();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var list = ok!.Value as List<ProgressEntity>;
        Assert.That(list, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetProgress_ExistingCourse_ReturnsProgress()
    {
        var course = await CreateCourse();
        await EnrollLearner(course.Id);
        Db.Progresses.Add(new ProgressEntity { LearnerId = LearnerId, CourseId = course.Id, PercentageComplete = 70, LastUpdated = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await _sut.GetProgress(course.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var progress = ok!.Value as ProgressEntity;
        Assert.That(progress!.PercentageComplete, Is.EqualTo(70));
    }

    [Test]
    public async Task GetProgress_NotEnrolled_ReturnsNotFound()
    {
        var course = await CreateCourse();

        var result = await _sut.GetProgress(course.Id);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task UpdateProgress_NewProgress_CreatesRecord()
    {
        var course = await CreateCourse();
        await EnrollLearner(course.Id);

        var request = new UpdateProgressRequest(60, "Going well");
        var result = await _sut.UpdateProgress(course.Id, request);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var progress = ok!.Value as ProgressEntity;
        Assert.That(progress!.PercentageComplete, Is.EqualTo(60));
        Assert.That(progress.Notes, Is.EqualTo("Going well"));
    }

    [Test]
    public async Task UpdateProgress_ExistingProgress_UpdatesRecord()
    {
        var course = await CreateCourse();
        await EnrollLearner(course.Id);
        Db.Progresses.Add(new ProgressEntity { LearnerId = LearnerId, CourseId = course.Id, PercentageComplete = 30, LastUpdated = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var request = new UpdateProgressRequest(75, "Updated notes");
        var result = await _sut.UpdateProgress(course.Id, request);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var progress = ok!.Value as ProgressEntity;
        Assert.That(progress!.PercentageComplete, Is.EqualTo(75));
    }

    [Test]
    public async Task UpdateProgress_NotEnrolled_ReturnsNotFound()
    {
        var course = await CreateCourse();

        var request = new UpdateProgressRequest(50, null);
        var result = await _sut.UpdateProgress(course.Id, request);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task UpdateProgress_At100Percent_AutoIssuesCertificate()
    {
        var course = await CreateCourse("Completed Course");
        await EnrollLearner(course.Id);

        var request = new UpdateProgressRequest(100, null);
        var result = await _sut.UpdateProgress(course.Id, request);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);

        var cert = Db.Certificates.FirstOrDefault(c => c.LearnerId == LearnerId && c.CourseId == course.Id);
        Assert.That(cert, Is.Not.Null);
        Assert.That(cert!.CourseName, Is.EqualTo("Completed Course"));
        Assert.That(cert.CertificateNumber, Does.StartWith("CERT-"));

        // Enrollment should be marked complete
        var enrollment = Db.Enrollments.FirstOrDefault(e => e.LearnerId == LearnerId && e.CourseId == course.Id);
        Assert.That(enrollment!.CompletedAt, Is.Not.Null);
    }

    [Test]
    public async Task UpdateProgress_At100Percent_DoesNotDuplicateCertificate()
    {
        var course = await CreateCourse();
        await EnrollLearner(course.Id);
        // Already has a cert
        Db.Certificates.Add(new CertificateEntity
        {
            LearnerId = LearnerId,
            LearnerName = LearnerName,
            CourseId = course.Id,
            CourseName = course.Name,
            IssuedAt = DateTime.UtcNow,
            CertificateNumber = "CERT-2026-000001"
        });
        await Db.SaveChangesAsync();

        var request = new UpdateProgressRequest(100, null);
        await _sut.UpdateProgress(course.Id, request);

        var certCount = Db.Certificates.Count(c => c.LearnerId == LearnerId && c.CourseId == course.Id);
        Assert.That(certCount, Is.EqualTo(1));
    }
}
