using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Seniority threshold ruleset entry: a required minimum niveau for a skill within a profile/level.
/// FR38: thresholds are computed at read time — no background jobs.
/// </summary>
public class SeniorityThresholdEntity
{
    [Key]
    public int Id { get; set; }

    public CompetenceCentreProfile Profile { get; set; }

    public SeniorityLevel SeniorityLevel { get; set; }

    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    /// <summary>
    /// Minimum niveau (1-based) the consultant must reach for this threshold to be met.
    /// </summary>
    public int MinNiveau { get; set; }
}
