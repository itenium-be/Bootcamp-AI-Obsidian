using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A skill in the global itenium skill catalogue.
/// levelCount: 1 = checkbox (done/not done), 2–7 = progression scale.
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
    public required string Category { get; set; }

    /// <summary>
    /// 1 = checkbox, 2–7 = progression with level descriptors.
    /// </summary>
    public int LevelCount { get; set; } = 1;

    /// <summary>
    /// JSON array of level descriptor strings, indexed 1..LevelCount.
    /// Stored as JSON column in PostgreSQL.
    /// </summary>
    public string LevelDescriptorsJson { get; set; } = "[]";

    [NotMapped]
    public IList<string> LevelDescriptors
    {
        get => JsonSerializer.Deserialize<List<string>>(LevelDescriptorsJson) ?? [];
        set => LevelDescriptorsJson = JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// JSON array of prerequisite links: [{skillId, requiredNiveau}].
    /// </summary>
    public string PrerequisitesJson { get; set; } = "[]";

    [NotMapped]
    public IList<SkillPrerequisite> Prerequisites
    {
        get => JsonSerializer.Deserialize<List<SkillPrerequisite>>(PrerequisitesJson) ?? [];
        set => PrerequisitesJson = JsonSerializer.Serialize(value);
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SkillProfileEntity> SkillProfiles { get; set; } = [];

    public override string ToString() => $"{Name} ({Category})";
}

/// <summary>
/// Prerequisite link: a required skill at a minimum niveau.
/// </summary>
public class SkillPrerequisite
{
    public int SkillId { get; set; }
    public int RequiredNiveau { get; set; }
}
