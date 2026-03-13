using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class UserControllerTests : DatabaseTestBase
{
    private UserController _sut = null!;
    private ISkillForgeUser _user = null!;

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _user.IsBackOffice.Returns(true);
        _sut = new UserController(Db, _user);
    }

    [Test]
    public async Task GetUsers_AsBackOffice_ReturnsUsers()
    {
        // The Users table comes from AspNetIdentity. In test DB it's empty.
        var result = await _sut.GetUsers();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        // Just verify it returns successfully with a list (may be empty in test)
        Assert.That(ok!.Value, Is.Not.Null);
    }

    [Test]
    public async Task GetUser_WhenNotFound_ReturnsNotFound()
    {
        var result = await _sut.GetUser("nonexistent-id");
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }
}
