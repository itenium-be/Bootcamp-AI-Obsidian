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

    // --- GetUsers ---

    [Test]
    public async Task GetUsers_ReturnsOk()
    {
        _userManager.Users.Returns(new List<ForgeUser>
        {
            new() { Id = "1", Email = "a@test.com", FirstName = "A", LastName = "B" }
        }.AsQueryable());
        _userManager.GetRolesAsync(Arg.Any<ForgeUser>()).Returns(["learner"]);

        var result = await _sut.GetUsers();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetUsers_WhenNoUsers_ReturnsEmptyList()
    {
        _userManager.Users.Returns(new List<ForgeUser>().AsQueryable());

        var result = await _sut.GetUsers();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var list = ok!.Value as List<object>;
        Assert.That(list, Is.Empty);
    }

    // --- CreateUser ---

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
    public async Task CreateUser_WithInvalidRole_ReturnsBadRequest()
    {
        var request = new CreateUserRequest("test@test.com", "Test", "User", "Password123!", "superadmin", []);

        var result = await _sut.CreateUser(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<ForgeUser>(), Arg.Any<string>());
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
    public async Task CreateUser_WhenRoleAssignmentFails_ReturnsServerError()
    {
        var request = new CreateUserRequest("test@test.com", "Test", "User", "Password123!", "learner", []);
        _userManager.CreateAsync(Arg.Any<ForgeUser>(), request.Password)
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ForgeUser>(), request.Role)
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Role does not exist" }));

        var result = await _sut.CreateUser(request);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That((result as ObjectResult)!.StatusCode, Is.EqualTo(500));
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

    // --- UpdateUserRole ---

    [Test]
    public async Task UpdateUserRole_WhenValidRequest_ReturnsOk()
    {
        var user = new ForgeUser { Id = "1", Email = "a@test.com" };
        _userManager.FindByIdAsync("1").Returns(user);
        _userManager.GetRolesAsync(user).Returns(["learner"]);
        _userManager.RemoveFromRolesAsync(user, Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(user, "manager").Returns(IdentityResult.Success);

        var result = await _sut.UpdateUserRole("1", new UpdateUserRoleRequest("manager"));

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task UpdateUserRole_WhenInvalidRole_ReturnsBadRequest()
    {
        var result = await _sut.UpdateUserRole("1", new UpdateUserRoleRequest("superadmin"));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        await _userManager.DidNotReceive().FindByIdAsync(Arg.Any<string>());
    }

    [Test]
    public async Task UpdateUserRole_WhenUserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync("999").Returns((ForgeUser?)null);

        var result = await _sut.UpdateUserRole("999", new UpdateUserRoleRequest("learner"));

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task UpdateUserRole_RemovesExistingRoleAndAssignsNew()
    {
        var user = new ForgeUser { Id = "1", Email = "a@test.com" };
        _userManager.FindByIdAsync("1").Returns(user);
        _userManager.GetRolesAsync(user).Returns(["learner"]);
        _userManager.RemoveFromRolesAsync(user, Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(user, "backoffice").Returns(IdentityResult.Success);

        await _sut.UpdateUserRole("1", new UpdateUserRoleRequest("backoffice"));

        await _userManager.Received(1).RemoveFromRolesAsync(user, Arg.Is<IEnumerable<string>>(r => r.Contains("learner")));
        await _userManager.Received(1).AddToRoleAsync(user, "backoffice");
    }

    [Test]
    public async Task UpdateUserRole_WhenRemoveRolesFails_ReturnsServerError()
    {
        var user = new ForgeUser { Id = "1", Email = "a@test.com" };
        _userManager.FindByIdAsync("1").Returns(user);
        _userManager.GetRolesAsync(user).Returns(["learner"]);
        _userManager.RemoveFromRolesAsync(user, Arg.Any<IEnumerable<string>>())
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Remove failed" }));

        var result = await _sut.UpdateUserRole("1", new UpdateUserRoleRequest("manager"));

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That((result as ObjectResult)!.StatusCode, Is.EqualTo(500));
    }
}
