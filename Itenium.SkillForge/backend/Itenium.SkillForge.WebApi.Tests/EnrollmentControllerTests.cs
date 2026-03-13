using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class EnrollmentControllerTests : DatabaseTestBase
{
    private EnrollmentController _sut = null!;
    private ISkillForgeUser _user = null!;

    private const string LearnerId = "learner-user-id";
    private const string OtherLearnerId = "other-learner-id";

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _user.UserId.Returns(LearnerId);
        _user.IsBackOffice.Returns(false);
        _sut = new EnrollmentController(Db, _user);
    }

    private async Task<CourseEntity> CreateCourse(string name = "Test Course")
    {
        var course = new CourseEntity { Name = name };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        return course;
    }

    [Test]
    public async Task GetEnrollments_AsLearner_ReturnsOnlyMyEnrollments()
    {
        var course = await CreateCourse();
        Db.Enrollments.AddRange(
            new EnrollmentEntity { LearnerId = LearnerId, CourseId = course.Id },
            new EnrollmentEntity { LearnerId = OtherLearnerId, CourseId = course.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.GetEnrollments();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var list = ok!.Value as List<EnrollmentEntity>;
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list![0].LearnerId, Is.EqualTo(LearnerId));
    }

    [Test]
    public async Task GetEnrollments_AsBackOffice_ReturnsAll()
    {
        _user.IsBackOffice.Returns(true);
        var course = await CreateCourse();
        Db.Enrollments.AddRange(
            new EnrollmentEntity { LearnerId = LearnerId, CourseId = course.Id },
            new EnrollmentEntity { LearnerId = OtherLearnerId, CourseId = course.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.GetEnrollments();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var list = ok!.Value as List<EnrollmentEntity>;
        Assert.That(list, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Enroll_NewEnrollment_ReturnsCreated()
    {
        var course = await CreateCourse();

        var result = await _sut.Enroll(course.Id);

        var created = result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var enrollment = created!.Value as EnrollmentEntity;
        Assert.That(enrollment!.LearnerId, Is.EqualTo(LearnerId));
        Assert.That(enrollment.CourseId, Is.EqualTo(course.Id));

        var saved = await Db.Enrollments.FindAsync(enrollment.Id);
        Assert.That(saved, Is.Not.Null);
    }

    [Test]
    public async Task Enroll_DuplicateEnrollment_ReturnsConflict()
    {
        var course = await CreateCourse();
        Db.Enrollments.Add(new EnrollmentEntity { LearnerId = LearnerId, CourseId = course.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.Enroll(course.Id);

        Assert.That(result, Is.TypeOf<ConflictObjectResult>());
    }

    [Test]
    public async Task Enroll_CourseNotFound_ReturnsNotFound()
    {
        var result = await _sut.Enroll(9999);
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task Unenroll_ExistingEnrollment_ReturnsNoContent()
    {
        var course = await CreateCourse();
        Db.Enrollments.Add(new EnrollmentEntity { LearnerId = LearnerId, CourseId = course.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.Unenroll(course.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var remaining = Db.Enrollments.Where(e => e.LearnerId == LearnerId && e.CourseId == course.Id).ToList();
        Assert.That(remaining, Is.Empty);
    }

    [Test]
    public async Task Unenroll_NotEnrolled_ReturnsNotFound()
    {
        var course = await CreateCourse();

        var result = await _sut.Unenroll(course.Id);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}
