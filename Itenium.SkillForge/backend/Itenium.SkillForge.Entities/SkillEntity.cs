using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A skill in the global skill catalogue.
/// levelCount: 1 = checkbox (yes/no), 2-7 = progression levels.
/// </summary>
public class SkillEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// 1 = checkbox skill, 2-7 = multi-level progression.
    /// </summary>
    public int LevelCount { get; set; } = 1;

    /// <summary>
    /// True = universal itenium skill; False = profile-specific.
    /// </summary>
    public bool IsUniversal { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public IList<SkillLevelDescriptorEntity> LevelDescriptors { get; set; } = [];

    public IList<SkillPrerequisiteEntity> Prerequisites { get; set; } = [];

    public override string ToString() => $"{Name} ({Category})";
}
