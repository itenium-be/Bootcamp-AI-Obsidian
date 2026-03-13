using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Records a consultant marking a resource as completed, linked to a goal (FR23, FR30).
/// </summary>
public class ResourceCompletionEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid ResourceId { get; set; }

    public ResourceEntity Resource { get; set; } = null!;

    /// <summary>
    /// The consultant who completed the resource — maps to AspNetUsers.Id.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public required string ConsultantId { get; set; }

    /// <summary>
    /// The active goal this completion is recorded as evidence against.
    /// </summary>
    public Guid GoalId { get; set; }

    public GoalEntity Goal { get; set; } = null!;

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
