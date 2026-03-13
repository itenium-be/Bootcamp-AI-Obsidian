using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A thumbs-up or thumbs-down rating on a resource (FR24).
/// One rating per user per resource — upsert semantics.
/// </summary>
public class ResourceRatingEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid ResourceId { get; set; }

    public ResourceEntity Resource { get; set; } = null!;

    /// <summary>
    /// The user who submitted the rating — maps to AspNetUsers.Id.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    public ResourceRating Rating { get; set; }

    public DateTime RatedAt { get; set; } = DateTime.UtcNow;
}

public enum ResourceRating
{
    ThumbsUp = 1,
    ThumbsDown = 2,
}
