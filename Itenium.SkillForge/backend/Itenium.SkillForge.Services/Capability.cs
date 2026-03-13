namespace Itenium.SkillForge.Services;

/// <summary>
/// Fine-grained capabilities for the SkillForge application.
/// Configure role-capability mappings in appsettings.json.
/// </summary>
public enum Capability
{
    ReadCourse,
    ManageCourse,
    ReadSkill,
    ManageSkill,
    ReadRoadmap,
    ManageConsultantProfile,
    ReadSeniority,

    /// <summary>
    /// Allows writing skill validations (FR4 — Coach role only).
    /// </summary>
    ValidateSkill,

    /// <summary>
    /// Allows platform-wide admin operations (FR3 — BackOffice only).
    /// </summary>
    ManageUsers,
}
