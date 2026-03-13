using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A prerequisite link: the skill requires another skill at a minimum level.
/// Non-blocking — used for warnings only.
/// </summary>
public class SkillPrerequisiteEntity
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The skill that has this prerequisite.
    /// </summary>
    public int SkillId { get; set; }

    [JsonIgnore]
    public SkillEntity Skill { get; set; } = null!;

    /// <summary>
    /// The skill that is required as a prerequisite.
    /// </summary>
    public int PrerequisiteSkillId { get; set; }

    public SkillEntity PrerequisiteSkill { get; set; } = null!;

    /// <summary>
    /// Minimum level of the prerequisite skill required. Default 1.
    /// </summary>
    public int RequiredLevel { get; set; } = 1;

    public override string ToString() => $"Requires skill {PrerequisiteSkillId} at level {RequiredLevel}";
}
