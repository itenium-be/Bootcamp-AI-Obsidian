using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Tracks a consultant's achieved level for a specific skill.
/// achievedLevel 0 = not started; 1+ = level reached.
/// </summary>
public class ConsultantSkillProgressEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    /// <summary>
    /// 0 = not started, 1–N = achieved level.
    /// </summary>
    public int AchievedLevel { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public override string ToString() => $"User {UserId} — Skill {SkillId} @ level {AchievedLevel}";
}
