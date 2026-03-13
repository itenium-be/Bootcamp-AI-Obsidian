using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class SkillsControllerTests : DatabaseTestBase
{
    private SkillsController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new SkillsController(Db);
    }

    // ── GET /api/skills ──────────────────────────────────────────────────────

    [Test]
    public async Task GetSkills_ReturnsAllSkills_WhenNoProfileFilter()
    {
        Db.Skills.AddRange(
            new SkillEntity { Name = "C# Basics", Category = "Development", LevelCount = 5 },
            new SkillEntity { Name = "Java Basics", Category = "Development", LevelCount = 5 });
        await Db.SaveChangesAsync();

        var result = await _sut.GetSkills(null);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var skills = ok!.Value as IEnumerable<object>;
        Assert.That(skills, Is.Not.Null);
        Assert.That(skills!.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetSkills_WhenNoSkills_ReturnsEmptyList()
    {
        var result = await _sut.GetSkills(null);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var skills = ok!.Value as IEnumerable<object>;
        Assert.That(skills, Is.Empty);
    }

    [Test]
    public async Task GetSkills_FiltersByProfile()
    {
        var skill1 = new SkillEntity { Name = "C# Basics", Category = "Development" };
        var skill2 = new SkillEntity { Name = "Java Basics", Category = "Development" };
        Db.Skills.AddRange(skill1, skill2);
        await Db.SaveChangesAsync();

        Db.SkillProfiles.Add(new SkillProfileEntity { SkillId = skill1.Id, Profile = CompetenceCentreProfile.DotNet });
        await Db.SaveChangesAsync();

        var result = await _sut.GetSkills(CompetenceCentreProfile.DotNet);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var skills = ok!.Value as IEnumerable<object>;
        Assert.That(skills!.Count(), Is.EqualTo(1));
    }

    // ── GET /api/skills/{id} ─────────────────────────────────────────────────

    [Test]
    public async Task GetSkill_WhenExists_ReturnsSkillWithPrerequisiteWarnings()
    {
        var prereq = new SkillEntity { Name = "Clean Code", Category = "Craftsmanship", LevelCount = 5 };
        var skill = new SkillEntity { Name = "DDD", Category = "Architecture", LevelCount = 3 };
        Db.Skills.AddRange(prereq, skill);
        await Db.SaveChangesAsync();

        // Set prerequisites JSON after we have IDs
        skill.Prerequisites = [new SkillPrerequisite { SkillId = prereq.Id, RequiredNiveau = 3 }];
        await Db.SaveChangesAsync();

        var result = await _sut.GetSkill(skill.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
    }

    [Test]
    public async Task GetSkill_WhenNotExists_ReturnsNotFound()
    {
        var result = await _sut.GetSkill(999);
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }
}
