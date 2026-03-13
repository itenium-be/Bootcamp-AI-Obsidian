using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A team is a CompetenceCenter like Java or PO-Analysis.
/// Teams scope manager access to courses and learners.
/// </summary>
public class TeamEntity
{
    /// <summary>Gets or sets the unique identifier for the team.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Gets or sets the team name (e.g. "Java", ".NET", "PO-Analysis"). Required, max 200 characters.</summary>
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>Gets or sets an optional description of the team and its focus area. Max 1000 characters.</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <inheritdoc/>
    public override string ToString() => Name;
}
