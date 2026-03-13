using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A certificate issued to a learner upon completing a course.
/// </summary>
public class CertificateEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string LearnerId { get; set; }

    [Required]
    [MaxLength(400)]
    public required string LearnerName { get; set; }

    public int CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public CourseEntity? Course { get; set; }

    /// <summary>
    /// Course name stored at issue time (snapshot).
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string CourseName { get; set; }

    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Unique certificate number, e.g. "CERT-2026-000001".
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string CertificateNumber { get; set; }
}
