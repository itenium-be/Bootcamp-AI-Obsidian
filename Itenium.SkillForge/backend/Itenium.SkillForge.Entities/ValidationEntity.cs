using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// An immutable skill-niveau validation record written by a coach (FR33, FR36).
/// Only manager role can write validations (server-side enforced).
/// </summary>
public class ValidationEntity
{
    [Key]
    public Guid Id { get; set; }

    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    /// <summary>
    /// The consultant whose skill was validated — maps to AspNetUsers.Id.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public required string ConsultantId { get; set; }

    /// <summary>
    /// The coach who validated — maps to AspNetUsers.Id.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public required string ValidatedBy { get; set; }

    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    public int FromNiveau { get; set; }

    public int ToNiveau { get; set; }

    /// <summary>
    /// Optional coaching session this validation was performed in.
    /// </summary>
    public Guid? SessionId { get; set; }

    public CoachingSessionEntity? Session { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
