using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Represents a learner's feedback (rating and comment) for a course they have completed.
/// </summary>
public class CourseFeedbackEntity
{
    /// <summary>Gets or sets the unique identifier for the feedback entry.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Gets or sets the Identity user ID of the learner who submitted the feedback.</summary>
    [Required]
    public required string LearnerId { get; set; }

    /// <summary>Gets or sets the identifier of the course being rated.</summary>
    public int CourseId { get; set; }

    /// <summary>Gets or sets the navigation property for the course.</summary>
    [ForeignKey(nameof(CourseId))]
    public CourseEntity? Course { get; set; }

    /// <summary>Gets or sets the rating value between 1 (lowest) and 5 (highest).</summary>
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>Gets or sets an optional free-text comment accompanying the rating.</summary>
    [MaxLength(2000)]
    public string? Comment { get; set; }

    /// <summary>Gets or sets the UTC date and time when the feedback was submitted.</summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
