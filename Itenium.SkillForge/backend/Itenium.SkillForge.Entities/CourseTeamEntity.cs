namespace Itenium.SkillForge.Entities;

/// <summary>
/// Represents the many-to-many relationship between courses and teams.
/// A course can be assigned to multiple teams, and a team can have multiple courses.
/// </summary>
public class CourseTeamEntity
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the course identifier.</summary>
    public int CourseId { get; set; }

    /// <summary>Gets or sets the team identifier.</summary>
    public int TeamId { get; set; }

    /// <summary>Gets or sets when this assignment was created.</summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the navigation property for the course.</summary>
    public CourseEntity? Course { get; set; }

    /// <summary>Gets or sets the navigation property for the team.</summary>
    public TeamEntity? Team { get; set; }
}
