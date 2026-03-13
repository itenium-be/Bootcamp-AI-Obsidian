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
    public async Task GetRoadmap_WhenConsultantHasProfile_ReturnsFilteredSkills()
    {
        // Arrange: 2 DotNet skills + 1 Java skill
        var dotNetSkill1 = new SkillEntity { Name = "C# Basics", Category = "Core" };
        var dotNetSkill2 = new SkillEntity { Name = "LINQ", Category = "Core" };
        var javaSkill = new SkillEntity { Name = "Java Basics", Category = "Core" };
        Db.Skills.AddRange(dotNetSkill1, dotNetSkill2, javaSkill);
        await Db.SaveChangesAsync();

        Db.SkillProfiles.AddRange(
            new SkillProfileEntity { SkillId = dotNetSkill1.Id, Profile = CompetenceCentreProfile.DotNet, SortOrder = 1 },
            new SkillProfileEntity { SkillId = dotNetSkill2.Id, Profile = CompetenceCentreProfile.DotNet, SortOrder = 2 },
            new SkillProfileEntity { SkillId = javaSkill.Id, Profile = CompetenceCentreProfile.Java, SortOrder = 1 });
        await Db.SaveChangesAsync();

        await CreateTestUser("user-dotnet");
        Db.ConsultantProfiles.Add(new ConsultantProfileEntity
        {
            UserId = "user-dotnet",
            Profile = CompetenceCentreProfile.DotNet,
        });
        await Db.SaveChangesAsync();

        // Act
        var result = await _sut.GetRoadmap("user-dotnet");

        // Assert
        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var response = ok!.Value as RoadmapResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Profile, Is.EqualTo(CompetenceCentreProfile.DotNet));
        Assert.That(response.Nodes, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetRoadmap_WhenNoProfile_ReturnsEmptyNodes()
    {
        var result = await _sut.GetRoadmap("user-noprofile");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var response = ok!.Value as RoadmapResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Nodes, Is.Empty);
    }

    [Test]
    public async Task GetRoadmap_DefaultView_ReturnsMaxTwelveNodes()
    {
        // Create 15 DotNet skills
        var skills = Enumerable.Range(1, 15)
            .Select(i => new SkillEntity { Name = $"Skill {i}", Category = "Core" })
            .ToList();
        Db.Skills.AddRange(skills);
        await Db.SaveChangesAsync();

        Db.SkillProfiles.AddRange(skills.Select((s, i) =>
            new SkillProfileEntity { SkillId = s.Id, Profile = CompetenceCentreProfile.DotNet, SortOrder = i + 1 }));
        await Db.SaveChangesAsync();

        await CreateTestUser("user-dotnet-2");
        Db.ConsultantProfiles.Add(new ConsultantProfileEntity
        {
            UserId = "user-dotnet-2",
            Profile = CompetenceCentreProfile.DotNet,
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetRoadmap("user-dotnet-2", showAll: false);

        var ok = result.Result as OkObjectResult;
        var response = ok!.Value as RoadmapResponse;
        Assert.That(response!.Nodes, Has.Count.LessThanOrEqualTo(12));
        Assert.That(response.TotalSkillCount, Is.EqualTo(15));
    }
}
