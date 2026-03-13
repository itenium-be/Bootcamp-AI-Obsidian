using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class RoadmapControllerTests : DatabaseTestBase
{
    private RoadmapController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new RoadmapController(Db);
    }

    [Test]
    public async Task GetRoadmap_ReturnsSkillsInConsultantProfile()
    {
        // Arrange: team with universal skills, consultant assigned to that team
        var team = new TeamEntity { Name = "Test Team" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var skill1 = new SkillEntity { Name = "Skill A", IsUniversal = true };
        var skill2 = new SkillEntity { Name = "Skill B", IsUniversal = true };
        Db.Skills.AddRange(skill1, skill2);

        Db.ConsultantProfiles.Add(new ConsultantProfileEntity { UserId = "roadmap-user", TeamId = team.Id });
        await Db.SaveChangesAsync();

        // Act
        var result = await _sut.GetRoadmap("roadmap-user");

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var roadmap = okResult!.Value as RoadmapResponse;
        Assert.That(roadmap, Is.Not.Null);
        Assert.That(roadmap!.Skills, Has.Count.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task UpdateProgress_UpdatesConsultantSkillLevel()
    {
        var skill = new SkillEntity { Name = "Progressable Skill", LevelCount = 5 };
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        var request = new UpdateProgressRequest(skill.Id, 3);

        var result = await _sut.UpdateProgress("progress-user", request);

        Assert.That(result, Is.TypeOf<NoContentResult>());

        var progress = Db.ConsultantSkillProgress
            .FirstOrDefault(p => p.UserId == "progress-user" && p.SkillId == skill.Id);
        Assert.That(progress, Is.Not.Null);
        Assert.That(progress!.AchievedLevel, Is.EqualTo(3));
    }
}
