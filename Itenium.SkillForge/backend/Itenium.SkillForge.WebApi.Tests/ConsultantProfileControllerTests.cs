using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class ConsultantProfileControllerTests : DatabaseTestBase
{
    private ConsultantProfileController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new ConsultantProfileController(Db);

        // Ensure the team exists (FK required)
        Db.Teams.Add(new TeamEntity { Id = 10, Name = "Test Team" });
        Db.SaveChanges();
    }

    [Test]
    public async Task AssignProfile_CreatesProfile()
    {
        var request = new AssignProfileRequest("user-abc", 10);

        var result = await _sut.AssignProfile(request);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var profile = okResult!.Value as ConsultantProfileEntity;
        Assert.That(profile!.UserId, Is.EqualTo("user-abc"));
        Assert.That(profile.TeamId, Is.EqualTo(10));

        var saved = await Db.ConsultantProfiles.FindAsync(profile.Id);
        Assert.That(saved, Is.Not.Null);
    }

    [Test]
    public async Task AssignProfile_WhenAlreadyAssigned_UpdatesProfile()
    {
        Db.Teams.Add(new TeamEntity { Id = 11, Name = "New Team" });
        await Db.SaveChangesAsync();

        var existing = new ConsultantProfileEntity { UserId = "user-xyz", TeamId = 10 };
        Db.ConsultantProfiles.Add(existing);
        await Db.SaveChangesAsync();

        var request = new AssignProfileRequest("user-xyz", 11);

        var result = await _sut.AssignProfile(request);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var profile = okResult!.Value as ConsultantProfileEntity;
        Assert.That(profile!.TeamId, Is.EqualTo(11));
        Assert.That(profile.UserId, Is.EqualTo("user-xyz"));
    }

    [Test]
    public async Task GetProfile_WhenAssigned_ReturnsProfile()
    {
        var profile = new ConsultantProfileEntity { UserId = "user-get", TeamId = 10 };
        Db.ConsultantProfiles.Add(profile);
        await Db.SaveChangesAsync();

        var result = await _sut.GetProfile("user-get");

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var returned = okResult!.Value as ConsultantProfileEntity;
        Assert.That(returned!.UserId, Is.EqualTo("user-get"));
        Assert.That(returned.TeamId, Is.EqualTo(10));
    }

    [Test]
    public async Task GetProfile_WhenNotAssigned_ReturnsNotFound()
    {
        var result = await _sut.GetProfile("no-such-user");
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }
}
