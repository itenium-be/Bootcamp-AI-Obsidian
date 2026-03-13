using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Readiness flag raised by a consultant on an active goal (FR18–FR20).
/// Maximum one active flag per goal at a time.
/// </summary>
public class ReadinessFlagEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid GoalId { get; set; }

    public GoalEntity Goal { get; set; } = null!;

    /// <summary>
    /// The consultant who raised the flag — maps to AspNetUsers.Id.
    /// </summary>
    [Required]
    public required string ConsultantId { get; set; }

    /// <summary>
    /// Timestamp when the flag was raised (FR19).
    /// </summary>
    public DateTime RaisedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// True while the flag is still active (not yet lowered by the consultant).
    /// </summary>
    public bool IsActive { get; set; } = true;

    public override string ToString() => $"ReadinessFlag(GoalId={GoalId}, Active={IsActive})";
}
