using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Records a skill validation performed by a coach (manager role).
/// ValidatedBy and ValidatedAt are immutable once written (FR36).
/// </summary>
public class SkillValidationEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string SkillName { get; set; }

    [Required]
    [MaxLength(450)]
    public required string LearnerId { get; set; }

    /// <summary>
    /// The coach user ID who performed the validation. Immutable once written (FR36).
    /// </summary>
    [Required]
    [MaxLength(450)]
    public required string ValidatedBy { get; set; }

    /// <summary>
    /// The UTC timestamp when the validation was recorded. Immutable once written (FR36).
    /// </summary>
    public DateTime ValidatedAt { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Level { get; set; }
}
