using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Tracks a learner's progress in a course (0-100%).
/// </summary>
public class ProgressEntity
{
    /// <summary>Gets or sets the unique identifier for the progress record.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Gets or sets the Identity user ID of the learner.</summary>
    [Required]
    public required string LearnerId { get; set; }

    /// <summary>Gets or sets the identifier of the course being progressed through.</summary>
    public int CourseId { get; set; }

    /// <summary>Gets or sets the navigation property for the course.</summary>
    [ForeignKey(nameof(CourseId))]
    public CourseEntity? Course { get; set; }

    /// <summary>
    /// Gets or sets the completion percentage from 0 to 100.
    /// </summary>
    public int PercentageComplete { get; set; }

    /// <summary>Gets or sets the UTC date and time when progress was last updated.</summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>Gets or sets optional freeform notes from the learner. Max 2000 characters.</summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }
}
