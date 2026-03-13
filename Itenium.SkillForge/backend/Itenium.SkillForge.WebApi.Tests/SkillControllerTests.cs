using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class SkillControllerTests : DatabaseTestBase
{
    private SkillController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new SkillController(Db);
    }

    [Test]
    public async Task GetSkills_ReturnsAllSkills()
    {
        Db.Skills.AddRange(
            new SkillEntity { Name = "C# Basics", Category = "Backend" },
            new SkillEntity { Name = "Docker", Category = "DevOps" });
        await Db.SaveChangesAsync();

        var result = await _sut.GetSkills();

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var skills = okResult!.Value as List<SkillEntity>;
        Assert.That(skills, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetSkill_WhenExists_ReturnsSkillWithDescriptors()
    {
        var skill = new SkillEntity
        {
            Name = "Unit Testing",
            Category = "Quality",
            LevelCount = 3,
            LevelDescriptors =
            [
                new SkillLevelDescriptorEntity { Level = 1, Description = "Writes basic tests" },
                new SkillLevelDescriptorEntity { Level = 2, Description = "Uses mocking" },
                new SkillLevelDescriptorEntity { Level = 3, Description = "Practices TDD" },
            ],
        };
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        var result = await _sut.GetSkill(skill.Id);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var returned = okResult!.Value as SkillEntity;
        Assert.That(returned!.Name, Is.EqualTo("Unit Testing"));
        Assert.That(returned.LevelDescriptors, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetSkill_WhenNotExists_ReturnsNotFound()
    {
        var result = await _sut.GetSkill(999);
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task CreateSkill_AddsSkillAndReturnsCreated()
    {
        var request = new CreateSkillRequest(
            "New Skill",
            "A description",
            "Category",
            LevelCount: 3,
            IsUniversal: true,
            LevelDescriptors: []);

        var result = await _sut.CreateSkill(request);

        var createdResult = result.Result as CreatedAtActionResult;
        Assert.That(createdResult, Is.Not.Null);
        var skill = createdResult!.Value as SkillEntity;
        Assert.That(skill!.Name, Is.EqualTo("New Skill"));
        Assert.That(skill.Category, Is.EqualTo("Category"));

        var saved = await Db.Skills.FindAsync(skill.Id);
        Assert.That(saved, Is.Not.Null);
    }

    [Test]
    public async Task DeleteSkill_WhenExists_RemovesAndReturnsNoContent()
    {
        var skill = new SkillEntity { Name = "To Delete" };
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        var result = await _sut.DeleteSkill(skill.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var deleted = await Db.Skills.FindAsync(skill.Id);
        Assert.That(deleted, Is.Null);
    }
}
