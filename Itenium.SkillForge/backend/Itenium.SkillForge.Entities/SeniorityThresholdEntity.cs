using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Defines the minimum skill level required for a given seniority level within a team profile.
/// </summary>
public class SeniorityThresholdEntity
{
    [Key]
    public int Id { get; set; }

    public int TeamId { get; set; }

    public TeamEntity Team { get; set; } = null!;

    public SeniorityLevel SeniorityLevel { get; set; }

    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    /// <summary>
    /// The minimum level the consultant must have achieved for this skill.
    /// </summary>
    public int MinimumLevel { get; set; }

    public override string ToString() => $"Team {TeamId} — {SeniorityLevel}: Skill {SkillId} >= {MinimumLevel}";
}
