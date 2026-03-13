using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Unit tests for the FeedbackController.
/// Uses an in-process Testcontainers PostgreSQL database.
/// </summary>
[TestFixture]
public class FeedbackControllerTests : DatabaseTestBase
{
    private FeedbackController _sut = null!;
    private ISkillForgeUser _user = null!;

    private const string LearnerId = "learner-user-id";
    private const string OtherLearnerId = "other-learner-id";

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _user.UserId.Returns(LearnerId);
        _user.IsBackOffice.Returns(false);
        _sut = new FeedbackController(Db, _user);
    }

    private async Task<CourseEntity> CreateCourse(string name = "Test Course")
    {
        var course = new CourseEntity { Name = name };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        return course;
    }

    private async Task EnrollAndComplete(string learnerId, int courseId)
    {
        Db.Enrollments.Add(new EnrollmentEntity
        {
            LearnerId = learnerId,
            CourseId = courseId,
            EnrolledAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        });
        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task SubmitFeedback_WhenEnrolledAndCompleted_CreatesRecord()
    {
        var course = await CreateCourse();
        await EnrollAndComplete(LearnerId, course.Id);

        var request = new SubmitFeedbackRequest(course.Id, 4, "Great course!");
        var result = await _sut.SubmitFeedback(request);

        Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task SubmitFeedback_WhenNotEnrolled_ReturnsBadRequest()
    {
        var course = await CreateCourse();

        var request = new SubmitFeedbackRequest(course.Id, 5, null);
        var result = await _sut.SubmitFeedback(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SubmitFeedback_WhenEnrolledButNotCompleted_ReturnsBadRequest()
    {
        var course = await CreateCourse();
        Db.Enrollments.Add(new EnrollmentEntity
        {
            LearnerId = LearnerId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        });
        await Db.SaveChangesAsync();

        var request = new SubmitFeedbackRequest(course.Id, 3, "not done yet");
        var result = await _sut.SubmitFeedback(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SubmitFeedback_WhenAlreadySubmitted_ReturnsConflict()
    {
        var course = await CreateCourse();
        await EnrollAndComplete(LearnerId, course.Id);

        // First submission
        Db.CourseFeedbacks.Add(new CourseFeedbackEntity
        {
            LearnerId = LearnerId,
            CourseId = course.Id,
            Rating = 4,
            SubmittedAt = DateTime.UtcNow
        });
        await Db.SaveChangesAsync();

        var request = new SubmitFeedbackRequest(course.Id, 5, "Duplicate");
        var result = await _sut.SubmitFeedback(request);

        Assert.That(result.Result, Is.TypeOf<ConflictObjectResult>());
    }

    [Test]
    public async Task GetFeedback_AsLearner_ReturnsOwnFeedbackOnly()
    {
        var course = await CreateCourse();
        await EnrollAndComplete(LearnerId, course.Id);

        Db.CourseFeedbacks.AddRange(
            new CourseFeedbackEntity { LearnerId = LearnerId, CourseId = course.Id, Rating = 5, SubmittedAt = DateTime.UtcNow },
            new CourseFeedbackEntity { LearnerId = OtherLearnerId, CourseId = course.Id, Rating = 3, SubmittedAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await _sut.GetFeedback();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var feedbacks = ok!.Value as IList<CourseFeedbackEntity>;
        Assert.That(feedbacks, Has.Count.EqualTo(1));
        Assert.That(feedbacks![0].LearnerId, Is.EqualTo(LearnerId));
    }

    [Test]
    public async Task GetFeedback_AsBackOffice_ReturnsAllFeedback()
    {
        _user.IsBackOffice.Returns(true);

        var course = await CreateCourse();
        await EnrollAndComplete(LearnerId, course.Id);

        Db.CourseFeedbacks.AddRange(
            new CourseFeedbackEntity { LearnerId = LearnerId, CourseId = course.Id, Rating = 5, SubmittedAt = DateTime.UtcNow },
            new CourseFeedbackEntity { LearnerId = OtherLearnerId, CourseId = course.Id, Rating = 3, SubmittedAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await _sut.GetFeedback();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var feedbacks = ok!.Value as IList<CourseFeedbackEntity>;
        Assert.That(feedbacks, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetCourseFeedback_ReturnsSummaryWithAverageAndList()
    {
        var course = await CreateCourse("My Course");
        Db.CourseFeedbacks.AddRange(
            new CourseFeedbackEntity { LearnerId = LearnerId, CourseId = course.Id, Rating = 4, Comment = "Good", SubmittedAt = DateTime.UtcNow },
            new CourseFeedbackEntity { LearnerId = OtherLearnerId, CourseId = course.Id, Rating = 2, Comment = "Meh", SubmittedAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await _sut.GetCourseFeedback(course.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var response = ok!.Value as CourseFeedbackResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Count, Is.EqualTo(2));
        Assert.That(response.AverageRating, Is.EqualTo(3.0).Within(0.01));
    }

    [Test]
    public async Task GetFeedbackSummary_WhenNotBackOffice_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);

        var result = await _sut.GetFeedbackSummary();

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task GetFeedbackSummary_AsBackOffice_ReturnsAveragePerCourse()
    {
        _user.IsBackOffice.Returns(true);

        var course1 = await CreateCourse("Course A");
        var course2 = await CreateCourse("Course B");

        Db.CourseFeedbacks.AddRange(
            new CourseFeedbackEntity { LearnerId = LearnerId, CourseId = course1.Id, Rating = 5, SubmittedAt = DateTime.UtcNow },
            new CourseFeedbackEntity { LearnerId = OtherLearnerId, CourseId = course1.Id, Rating = 3, SubmittedAt = DateTime.UtcNow },
            new CourseFeedbackEntity { LearnerId = LearnerId, CourseId = course2.Id, Rating = 4, SubmittedAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await _sut.GetFeedbackSummary();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var summaries = ok!.Value as IList<FeedbackSummaryItem>;
        Assert.That(summaries, Has.Count.EqualTo(2));

        var course1Summary = summaries!.First(s => s.CourseId == course1.Id);
        Assert.That(course1Summary.AverageRating, Is.EqualTo(4.0).Within(0.01));
        Assert.That(course1Summary.FeedbackCount, Is.EqualTo(2));
    }
}
