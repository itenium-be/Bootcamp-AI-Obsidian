namespace Itenium.SkillForge.WebApi.Controllers;

public record StatsResponse(
    int TotalCourses,
    int TotalLearners,
    int TotalEnrollments,
    int TotalCertificates,
    double CompletionRate);
