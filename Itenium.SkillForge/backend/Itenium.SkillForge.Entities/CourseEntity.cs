using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Course master data managed by central management.
/// Courses can be assigned to teams and enrolled in by learners.
/// </summary>
public class CourseEntity
{
    /// <summary>Gets or sets the unique identifier for the course.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Gets or sets the course name. Required, max 200 characters.</summary>
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>Gets or sets an optional detailed description of the course. Max 2000 characters.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Gets or sets the category this course belongs to (e.g. "Programming", "Management"). Max 100 characters.</summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>Gets or sets the skill level of this course (e.g. "Beginner", "Advanced"). Max 50 characters.</summary>
    [MaxLength(50)]
    public string? Level { get; set; }

    /// <summary>Gets or sets the UTC date and time when this course was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public override string ToString() => $"{Name} ({Category})";
}
