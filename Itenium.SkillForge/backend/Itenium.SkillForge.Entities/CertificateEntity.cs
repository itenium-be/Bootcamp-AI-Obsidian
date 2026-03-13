using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A certificate issued to a learner upon completing a course.
/// </summary>
public class CertificateEntity
{
    /// <summary>Gets or sets the unique identifier for the certificate.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Gets or sets the Identity user ID of the learner who earned the certificate.</summary>
    [Required]
    public required string LearnerId { get; set; }

    /// <summary>Gets or sets the display name of the learner at the time of issue (snapshot). Max 400 characters.</summary>
    [Required]
    [MaxLength(400)]
    public required string LearnerName { get; set; }

    /// <summary>Gets or sets the identifier of the completed course.</summary>
    public int CourseId { get; set; }

    /// <summary>Gets or sets the navigation property for the course.</summary>
    [ForeignKey(nameof(CourseId))]
    public CourseEntity? Course { get; set; }

    /// <summary>
    /// Gets or sets the course name stored at issue time (snapshot). Max 200 characters.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string CourseName { get; set; }

    /// <summary>Gets or sets the UTC date and time when this certificate was issued.</summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Gets or sets the unique certificate number, e.g. "CERT-2026-000001". Max 50 characters.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string CertificateNumber { get; set; }
}
