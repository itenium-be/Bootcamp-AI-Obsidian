using System.ComponentModel.DataAnnotations;
using Itenium.Forge.Security.OpenIddict;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Assigns a consultant (learner) to a competence centre profile.
/// Created by a coach/manager for Story #19.
/// </summary>
public class ConsultantProfileEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string UserId { get; set; }

    public ForgeUser User { get; set; } = null!;

    public CompetenceCentreProfile Profile { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? AssignedBy { get; set; }
}
