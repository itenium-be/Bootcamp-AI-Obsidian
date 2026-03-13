using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Assigns a consultant (userId) to a competence centre profile (teamId).
/// </summary>
public class ConsultantProfileEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    public int TeamId { get; set; }

    public TeamEntity Team { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public override string ToString() => $"User {UserId} → Team {TeamId}";
}
