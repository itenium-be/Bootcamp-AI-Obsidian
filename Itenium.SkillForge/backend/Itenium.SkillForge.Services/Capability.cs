namespace Itenium.SkillForge.Services;

/// <summary>
/// Fine-grained capabilities for the SkillForge application.
/// Configure role-capability mappings in appsettings.json.
/// </summary>
public enum Capability
{
    /// <summary>Allows reading course data. Granted to all authenticated users.</summary>
    ReadCourse,

    /// <summary>Allows creating, updating, and deleting courses. Granted to BackOffice and managers.</summary>
    ManageCourse,

    /// <summary>Allows enrolling in and unenrolling from courses. Granted to learners.</summary>
    Enroll,

    /// <summary>Allows managing user accounts and roles. Granted to BackOffice only.</summary>
    ManageUsers,
}
