using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Manages learner feedback (ratings and comments) for completed courses.
/// Learners can submit and view their own feedback; BackOffice can view all feedback and summaries.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    /// <summary>
    /// Initializes a new instance of <see cref="FeedbackController"/>.
    /// </summary>
    /// <param name="db">The application database context.</param>
    /// <param name="user">The current authenticated user abstraction.</param>
    public FeedbackController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get feedback entries. BackOffice sees all; learners see only their own.
    /// </summary>
    /// <returns>A list of feedback entries scoped to the current user or all entries for BackOffice.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IList<CourseFeedbackEntity>>> GetFeedback()
    {
        var query = _db.CourseFeedbacks.AsQueryable();

        if (!_user.IsBackOffice)
        {
            query = query.Where(f => f.LearnerId == _user.UserId);
        }

        return Ok(await query.ToListAsync());
    }

    /// <summary>
    /// Get aggregated feedback for a specific course including average rating, count, and individual entries.
    /// </summary>
    /// <param name="courseId">The unique identifier of the course.</param>
    /// <returns>Aggregated feedback with average rating, total count, and full list of feedback items.</returns>
    [HttpGet("course/{courseId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseFeedbackResponse>> GetCourseFeedback(int courseId)
    {
        var course = await _db.Courses.FindAsync(courseId);
        if (course == null)
        {
            return NotFound();
        }

        var feedbacks = await _db.CourseFeedbacks
            .Where(f => f.CourseId == courseId)
            .ToListAsync();

        double avgRating = feedbacks.Count > 0
            ? Math.Round(feedbacks.Average(f => (double)f.Rating), 2)
            : 0.0;

        var items = feedbacks
            .Select(f => new FeedbackItem(f.Id, f.LearnerId, f.Rating, f.Comment, f.SubmittedAt))
            .ToList();

        return Ok(new CourseFeedbackResponse(courseId, course.Name, avgRating, feedbacks.Count, items));
    }

    /// <summary>
    /// Submit a rating and optional comment for a course the learner has completed (Learner only).
    /// The learner must be enrolled and have completed the course before submitting feedback.
    /// Only one feedback submission per learner per course is allowed.
    /// </summary>
    /// <param name="request">The feedback submission containing course ID, rating (1-5), and optional comment.</param>
    /// <returns>The newly created feedback entry.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseFeedbackEntity>> SubmitFeedback([FromBody] SubmitFeedbackRequest request)
    {
        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.LearnerId == _user.UserId && e.CourseId == request.CourseId);

        if (enrollment == null)
        {
            return BadRequest("You must be enrolled in the course to submit feedback.");
        }

        if (enrollment.CompletedAt == null)
        {
            return BadRequest("You must complete the course before submitting feedback.");
        }

        var existing = await _db.CourseFeedbacks
            .FirstOrDefaultAsync(f => f.LearnerId == _user.UserId && f.CourseId == request.CourseId);

        if (existing != null)
        {
            return Conflict("You have already submitted feedback for this course.");
        }

        var feedback = new CourseFeedbackEntity
        {
            LearnerId = _user.UserId!,
            CourseId = request.CourseId,
            Rating = request.Rating,
            Comment = request.Comment,
            SubmittedAt = DateTime.UtcNow
        };

        _db.CourseFeedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCourseFeedback), new { courseId = request.CourseId }, feedback);
    }

    /// <summary>
    /// Get a summary of average ratings per course (BackOffice and managers only).
    /// </summary>
    /// <returns>A list of feedback summary items with average rating and feedback count per course.</returns>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IList<FeedbackSummaryItem>>> GetFeedbackSummary()
    {
        if (!_user.IsBackOffice && _user.Teams.Count == 0)
        {
            return Forbid();
        }

        var summaries = await _db.CourseFeedbacks
            .Include(f => f.Course)
            .GroupBy(f => new { f.CourseId, CourseName = f.Course!.Name })
            .Select(g => new FeedbackSummaryItem(
                g.Key.CourseId,
                g.Key.CourseName,
                Math.Round(g.Average(f => (double)f.Rating), 2),
                g.Count()))
            .ToListAsync();

        return Ok(summaries);
    }
}
