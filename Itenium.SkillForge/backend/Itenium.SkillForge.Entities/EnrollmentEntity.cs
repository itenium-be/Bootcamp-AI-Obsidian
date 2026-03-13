using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Tracks a learner's enrollment in a course.
/// </summary>
public class EnrollmentEntity
{
    /// <summary>Gets or sets the unique identifier for the enrollment.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Gets or sets the Identity user ID of the enrolled learner.</summary>
    [Required]
    public required string LearnerId { get; set; }

    /// <summary>Gets or sets the identifier of the enrolled course.</summary>
    public int CourseId { get; set; }

    /// <summary>Gets or sets the navigation property for the course.</summary>
    [ForeignKey(nameof(CourseId))]
    public CourseEntity? Course { get; set; }

    /// <summary>Gets or sets the UTC date and time when the learner enrolled.</summary>
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC date and time when the learner completed the course, or null if not yet completed.</summary>
    public DateTime? CompletedAt { get; set; }
}
