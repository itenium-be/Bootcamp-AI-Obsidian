using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Request body for submitting feedback on a completed course.
/// </summary>
/// <param name="CourseId">The identifier of the course being rated.</param>
/// <param name="Rating">The rating value (1 = lowest, 5 = highest).</param>
/// <param name="Comment">An optional free-text comment accompanying the rating.</param>
public record SubmitFeedbackRequest(
    int CourseId,
    [Range(1, 5)] int Rating,
    [MaxLength(2000)] string? Comment);
