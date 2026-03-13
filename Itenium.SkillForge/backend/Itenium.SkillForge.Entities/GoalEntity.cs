using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A learning goal set by a coach for a consultant (FR16, FR35).
/// </summary>
public class GoalEntity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The consultant (learner) this goal belongs to — maps to AspNetUsers.Id.
    /// </summary>
    [Required]
    public required string ConsultantId { get; set; }

    /// <summary>
    /// The coach (manager) who created this goal — maps to AspNetUsers.Id.
    /// </summary>
    [Required]
    public required string CoachId { get; set; }

    /// <summary>
    /// The skill this goal targets.
    /// </summary>
    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    /// <summary>
    /// The consultant's current proficiency niveau (1-based).
    /// </summary>
    public int CurrentNiveau { get; set; }

    /// <summary>
    /// The target proficiency niveau (1-based).
    /// </summary>
    public int TargetNiveau { get; set; }

    public DateTime Deadline { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public GoalStatus Status { get; set; } = GoalStatus.Active;

    /// <summary>
    /// Comma-separated resource IDs linked to this goal.
    /// </summary>
    [MaxLength(2000)]
    public string? LinkedResourceIds { get; set; }

    public ICollection<ReadinessFlagEntity> ReadinessFlags { get; set; } = [];

    public override string ToString() => $"Goal({SkillId}: {CurrentNiveau}→{TargetNiveau})";
}

public enum GoalStatus
{
    Active = 1,
    Completed = 2,
    Cancelled = 3,
}
