using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Tracks a learner's progress in a course (0-100%).
/// </summary>
public class ProgressEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string LearnerId { get; set; }

    public int CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public CourseEntity? Course { get; set; }

    /// <summary>
    /// Completion percentage from 0 to 100.
    /// </summary>
    public int PercentageComplete { get; set; }

    public DateTime LastUpdated { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
