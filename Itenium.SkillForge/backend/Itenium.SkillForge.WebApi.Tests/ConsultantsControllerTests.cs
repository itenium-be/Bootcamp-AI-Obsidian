using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class ConsultantsControllerTests : DatabaseTestBase
{
    private ConsultantsController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new ConsultantsController(Db);
    }

    [Test]
    public async Task AssignProfile_NewConsultant_CreatesProfile()
    {
        var userId = "user-abc";
        await CreateTestUser(userId);
        var request = new AssignProfileRequest(CompetenceCentreProfile.Java, "coach-1");

        var result = await _sut.AssignProfile(userId, request);

        Assert.That(result, Is.TypeOf<OkResult>());
        var saved = Db.ConsultantProfiles.FirstOrDefault(cp => cp.UserId == userId);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Profile, Is.EqualTo(CompetenceCentreProfile.Java));
    }

    [Test]
    public async Task AssignProfile_ExistingConsultant_UpdatesProfile()
    {
        var userId = "user-xyz";
        await CreateTestUser(userId);
        Db.ConsultantProfiles.Add(new ConsultantProfileEntity
        {
            UserId = userId,
            Profile = CompetenceCentreProfile.DotNet,
        });
        await Db.SaveChangesAsync();

        var request = new AssignProfileRequest(CompetenceCentreProfile.Java, "coach-1");
        var result = await _sut.AssignProfile(userId, request);

        Assert.That(result, Is.TypeOf<OkResult>());
        var saved = Db.ConsultantProfiles.FirstOrDefault(cp => cp.UserId == userId);
        Assert.That(saved!.Profile, Is.EqualTo(CompetenceCentreProfile.Java));
    }

    [Test]
    public async Task GetConsultantProfile_WhenExists_ReturnsProfile()
    {
        var userId = "user-lea";
        await CreateTestUser(userId);
        Db.ConsultantProfiles.Add(new ConsultantProfileEntity
        {
            UserId = userId,
            Profile = CompetenceCentreProfile.DotNet,
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetConsultantProfile(userId);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
    }

    [Test]
    public async Task GetConsultantProfile_WhenNotExists_ReturnsNotFound()
    {
        var result = await _sut.GetConsultantProfile("nonexistent-user");
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }
}
