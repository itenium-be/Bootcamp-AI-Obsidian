using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class SeniorityControllerTests : DatabaseTestBase
{
    private SeniorityController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new SeniorityController(Db);
    }

    [Test]
    public async Task GetProgress_ReturnsProgressForAllLevels()
    {
        var team = new TeamEntity { Name = "Seniority Team" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var skill = new SkillEntity { Name = "Core Skill", LevelCount = 3 };
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        Db.SeniorityThresholds.Add(new SeniorityThresholdEntity
        {
            TeamId = team.Id,
            SeniorityLevel = SeniorityLevel.Junior,
            SkillId = skill.Id,
            MinimumLevel = 1,
        });
        Db.ConsultantProfiles.Add(new ConsultantProfileEntity { UserId = "seniority-user", TeamId = team.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.GetProgress("seniority-user");

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var response = okResult!.Value as List<SeniorityProgressItem>;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!, Has.Count.EqualTo(3)); // Junior, Medior, Senior
    }

    [Test]
    public async Task GetProgress_WhenRequirementsMet_ShowsCorrectCount()
    {
        var team = new TeamEntity { Name = "Met Team" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var skill = new SkillEntity { Name = "Met Skill", LevelCount = 3 };
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        Db.SeniorityThresholds.Add(new SeniorityThresholdEntity
        {
            TeamId = team.Id,
            SeniorityLevel = SeniorityLevel.Junior,
            SkillId = skill.Id,
            MinimumLevel = 1,
        });

        // Consultant has achieved level 2 for this skill
        Db.ConsultantSkillProgress.Add(new ConsultantSkillProgressEntity
        {
            UserId = "met-user",
            SkillId = skill.Id,
            AchievedLevel = 2,
        });
        Db.ConsultantProfiles.Add(new ConsultantProfileEntity { UserId = "met-user", TeamId = team.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.GetProgress("met-user");

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var response = okResult!.Value as List<SeniorityProgressItem>;

        var juniorProgress = response!.First(r => r.Level == "Junior");
        Assert.That(juniorProgress.Met, Is.EqualTo(1));
        Assert.That(juniorProgress.Required, Is.EqualTo(1));
    }
}
