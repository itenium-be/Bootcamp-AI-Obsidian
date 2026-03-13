using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Links a skill to a competence centre profile (second layer in the two-layer skill architecture).
/// </summary>
public class SkillProfileEntity
{
    [Key]
    public int Id { get; set; }

    public int SkillId { get; set; }
    public SkillEntity Skill { get; set; } = null!;

    public CompetenceCentreProfile Profile { get; set; }

    /// <summary>
    /// Display order within the profile.
    /// </summary>
    public int SortOrder { get; set; }
}
