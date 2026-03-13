using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Describes what a specific level of a skill means.
/// </summary>
public class SkillLevelDescriptorEntity
{
    [Key]
    public int Id { get; set; }

    public int SkillId { get; set; }

    [JsonIgnore]
    public SkillEntity Skill { get; set; } = null!;

    /// <summary>
    /// The level number (1 to SkillEntity.LevelCount).
    /// </summary>
    public int Level { get; set; }

    [Required]
    [MaxLength(2000)]
    public required string Description { get; set; }

    public override string ToString() => $"Level {Level}: {Description}";
}
