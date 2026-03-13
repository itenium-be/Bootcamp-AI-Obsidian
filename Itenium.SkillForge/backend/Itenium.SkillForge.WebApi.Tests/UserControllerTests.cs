using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class UserControllerTests
{
    private UserManager<ForgeUser> _userManager = null!;
    private UserController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var store = Substitute.For<IUserStore<ForgeUser>>();
        _userManager = Substitute.For<UserManager<ForgeUser>>(
            store, null, null, null, null, null, null, null, null);
        _sut = new UserController(_userManager);
    }

    [TearDown]
    public void TearDown()
    {
        _userManager.Dispose();
    }

    [Test]
    public async Task CreateUser_WhenValidRequest_ReturnsCreated()
    {
        var request = new CreateUserRequest("test@test.com", "Test", "User", "Password123!", "learner", []);
        _userManager.CreateAsync(Arg.Any<ForgeUser>(), request.Password)
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ForgeUser>(), request.Role)
            .Returns(IdentityResult.Success);

        var result = await _sut.CreateUser(request);

        Assert.That(result, Is.InstanceOf<CreatedResult>());
    }

    [Test]
    public async Task CreateUser_WhenCreationFails_ReturnsBadRequest()
    {
        var request = new CreateUserRequest("test@test.com", "Test", "User", "weak", "learner", []);
        _userManager.CreateAsync(Arg.Any<ForgeUser>(), request.Password)
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        var result = await _sut.CreateUser(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateUser_WithTeamIds_AddsTeamClaims()
    {
        var request = new CreateUserRequest("test@test.com", "Test", "User", "Password123!", "manager", [1, 2]);
        _userManager.CreateAsync(Arg.Any<ForgeUser>(), request.Password)
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ForgeUser>(), request.Role)
            .Returns(IdentityResult.Success);
        _userManager.AddClaimAsync(Arg.Any<ForgeUser>(), Arg.Any<Claim>())
            .Returns(IdentityResult.Success);

        await _sut.CreateUser(request);

        await _userManager.Received(2).AddClaimAsync(
            Arg.Any<ForgeUser>(),
            Arg.Is<Claim>(c => c.Type == "team"));
    }

    [Test]
    public async Task CreateUser_AssignsRole()
    {
        var request = new CreateUserRequest("test@test.com", "Test", "User", "Password123!", "backoffice", []);
        _userManager.CreateAsync(Arg.Any<ForgeUser>(), request.Password)
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ForgeUser>(), request.Role)
            .Returns(IdentityResult.Success);

        await _sut.CreateUser(request);

        await _userManager.Received(1).AddToRoleAsync(Arg.Any<ForgeUser>(), "backoffice");
    }

    [Test]
    public async Task CreateUser_SetsUserEmailAndName()
    {
        ForgeUser? capturedUser = null;
        var request = new CreateUserRequest("alice@test.com", "Alice", "Smith", "Password123!", "learner", []);
        _userManager.CreateAsync(Arg.Do<ForgeUser>(u => capturedUser = u), request.Password)
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ForgeUser>(), request.Role)
            .Returns(IdentityResult.Success);

        await _sut.CreateUser(request);

        Assert.That(capturedUser!.Email, Is.EqualTo("alice@test.com"));
        Assert.That(capturedUser.FirstName, Is.EqualTo("Alice"));
        Assert.That(capturedUser.LastName, Is.EqualTo("Smith"));
    }
}
