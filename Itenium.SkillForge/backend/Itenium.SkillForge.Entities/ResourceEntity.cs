using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A learning resource in the shared library (FR21).
/// </summary>
public class ResourceEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public required string Title { get; set; }

    [Required]
    [MaxLength(2000)]
    public required string Url { get; set; }

    public ResourceType Type { get; set; }

    /// <summary>
    /// The skill this resource is related to.
    /// </summary>
    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    /// <summary>
    /// Minimum niveau (1-based) this resource targets.
    /// </summary>
    public int FromNiveau { get; set; } = 1;

    /// <summary>
    /// Maximum niveau (1-based) this resource targets.
    /// </summary>
    public int ToNiveau { get; set; } = 1;

    /// <summary>
    /// The user ID (AspNetUsers.Id) of the contributor.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public required string ContributedBy { get; set; }

    public DateTime ContributedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of thumbs-up ratings received.
    /// </summary>
    public int ThumbsUp { get; set; }

    /// <summary>
    /// Number of thumbs-down ratings received.
    /// </summary>
    public int ThumbsDown { get; set; }

    public ICollection<ResourceCompletionEntity> Completions { get; set; } = [];

    public ICollection<ResourceRatingEntity> Ratings { get; set; } = [];

    public override string ToString() => $"{Title} ({Type})";
}

public enum ResourceType
{
    Article = 1,
    Video = 2,
    Book = 3,
    Course = 4,
    Other = 5,
}
