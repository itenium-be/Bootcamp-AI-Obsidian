using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A focused live coaching session between a coach and a consultant (FR31, FR37).
/// </summary>
public class CoachingSessionEntity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The coach (manager) who started the session — maps to AspNetUsers.Id.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public required string CoachId { get; set; }

    /// <summary>
    /// The consultant being coached — maps to AspNetUsers.Id.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public required string ConsultantId { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    [MaxLength(5000)]
    public string? Notes { get; set; }

    public ICollection<ValidationEntity> Validations { get; set; } = [];
}
