namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Aggregated feedback summary for a specific course.
/// </summary>
/// <param name="CourseId">The identifier of the course.</param>
/// <param name="CourseName">The display name of the course.</param>
/// <param name="AverageRating">The average rating across all feedback entries (0 if no feedback yet).</param>
/// <param name="Count">The total number of feedback submissions for this course.</param>
/// <param name="Feedbacks">The individual feedback entries.</param>
public record CourseFeedbackResponse(
    int CourseId,
    string CourseName,
    double AverageRating,
    int Count,
    IList<FeedbackItem> Feedbacks);

/// <summary>
/// A single feedback item with rating, optional comment, and submission timestamp.
/// </summary>
/// <param name="Id">The feedback entry identifier.</param>
/// <param name="LearnerId">The user ID of the learner who submitted the feedback.</param>
/// <param name="Rating">The rating value (1-5).</param>
/// <param name="Comment">Optional free-text comment.</param>
/// <param name="SubmittedAt">UTC timestamp of when the feedback was submitted.</param>
public record FeedbackItem(
    int Id,
    string LearnerId,
    int Rating,
    string? Comment,
    DateTime SubmittedAt);

/// <summary>
/// Summary of the average rating per course, intended for management reports.
/// </summary>
/// <param name="CourseId">The identifier of the course.</param>
/// <param name="CourseName">The display name of the course.</param>
/// <param name="AverageRating">The average rating for this course (0 if no feedback).</param>
/// <param name="FeedbackCount">The total number of feedback submissions.</param>
public record FeedbackSummaryItem(
    int CourseId,
    string CourseName,
    double AverageRating,
    int FeedbackCount);
