namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Represents the dashboard statistics response for the SkillForge LMS.
/// </summary>
/// <param name="TotalCourses">Total number of courses in the system.</param>
/// <param name="TotalLearners">Total number of learners (all users for BackOffice; 1 for scoped users).</param>
/// <param name="TotalEnrollments">Total number of course enrollments.</param>
/// <param name="TotalCertificates">Total number of certificates issued.</param>
/// <param name="CompletionRate">Percentage of enrollments that have been completed (0-100).</param>
/// <param name="ActiveLearners">Number of learners enrolled in at least one course who have not yet completed all their enrollments.</param>
/// <param name="CoursesByCategory">Breakdown of course count grouped by category name.</param>
/// <param name="AverageProgress">Average completion percentage across all in-progress enrollments.</param>
public record StatsResponse(
    int TotalCourses,
    int TotalLearners,
    int TotalEnrollments,
    int TotalCertificates,
    double CompletionRate,
    int ActiveLearners,
    Dictionary<string, int> CoursesByCategory,
    double AverageProgress);
