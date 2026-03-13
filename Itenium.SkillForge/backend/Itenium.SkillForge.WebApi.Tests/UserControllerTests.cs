using System.Reflection;
using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class UserControllerAuthorizationTests
{
    // --- Issue #13: Admin login with platform-wide access ---
    // Verifies that UserController requires backoffice role at the API middleware level.

    [Test]
    public void UserController_RequiresBackofficeRole()
    {
        var type = typeof(UserController);
        var attr = type.GetCustomAttribute<AuthorizeAttribute>();
        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.Roles, Is.EqualTo("backoffice"));
    }
}

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
        var logger = Substitute.For<ILogger<UserController>>();
        _sut = new UserController(_userManager, logger);
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
        _userManager.GetClaimsAsync(Arg.Any<ForgeUser>()).Returns(Task.FromResult<IList<Claim>>(new List<Claim>()));

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

    // --- ArchiveUser (Issue #36) ---

    [Test]
    public async Task ArchiveUser_WhenUserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync("999").Returns(Task.FromResult<ForgeUser?>(null));

        var result = await _sut.ArchiveUser("999");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task ArchiveUser_WhenUserExists_ReturnsOk()
    {
        var user = new ForgeUser { Id = "1", Email = "a@test.com" };
        _userManager.FindByIdAsync("1").Returns(Task.FromResult<ForgeUser?>(user));
        _userManager.SetLockoutEnabledAsync(Arg.Any<ForgeUser>(), Arg.Any<bool>()).Returns(IdentityResult.Success);
        _userManager.SetLockoutEndDateAsync(Arg.Any<ForgeUser>(), Arg.Any<DateTimeOffset?>()).Returns(IdentityResult.Success);
        _userManager.AddClaimAsync(Arg.Any<ForgeUser>(), Arg.Any<Claim>()).Returns(IdentityResult.Success);

        var result = await _sut.ArchiveUser("1");

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task ArchiveUser_SetsLockoutEndToMaxValue()
    {
        var user = new ForgeUser { Id = "1", Email = "a@test.com" };
        _userManager.FindByIdAsync("1").Returns(Task.FromResult<ForgeUser?>(user));
        _userManager.SetLockoutEnabledAsync(Arg.Any<ForgeUser>(), Arg.Any<bool>()).Returns(IdentityResult.Success);
        _userManager.SetLockoutEndDateAsync(Arg.Any<ForgeUser>(), Arg.Any<DateTimeOffset?>()).Returns(IdentityResult.Success);
        _userManager.AddClaimAsync(Arg.Any<ForgeUser>(), Arg.Any<Claim>()).Returns(IdentityResult.Success);

        await _sut.ArchiveUser("1");

        await _userManager.Received(1).SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
    }

    [Test]
    public async Task ArchiveUser_SetsLockoutEnabled()
    {
        var user = new ForgeUser { Id = "1", Email = "a@test.com" };
        _userManager.FindByIdAsync("1").Returns(Task.FromResult<ForgeUser?>(user));
        _userManager.SetLockoutEnabledAsync(Arg.Any<ForgeUser>(), Arg.Any<bool>()).Returns(IdentityResult.Success);
        _userManager.SetLockoutEndDateAsync(Arg.Any<ForgeUser>(), Arg.Any<DateTimeOffset?>()).Returns(IdentityResult.Success);
        _userManager.AddClaimAsync(Arg.Any<ForgeUser>(), Arg.Any<Claim>()).Returns(IdentityResult.Success);

        await _sut.ArchiveUser("1");

        await _userManager.Received(1).SetLockoutEnabledAsync(user, true);
    }

    [Test]
    public async Task ArchiveUser_AddsArchivedClaim()
    {
        var user = new ForgeUser { Id = "1", Email = "a@test.com" };
        _userManager.FindByIdAsync("1").Returns(Task.FromResult<ForgeUser?>(user));
        _userManager.SetLockoutEnabledAsync(Arg.Any<ForgeUser>(), Arg.Any<bool>()).Returns(IdentityResult.Success);
        _userManager.SetLockoutEndDateAsync(Arg.Any<ForgeUser>(), Arg.Any<DateTimeOffset?>()).Returns(IdentityResult.Success);
        _userManager.AddClaimAsync(Arg.Any<ForgeUser>(), Arg.Any<Claim>()).Returns(IdentityResult.Success);

        await _sut.ArchiveUser("1");

        await _userManager.Received(1).AddClaimAsync(
            user,
            Arg.Is<Claim>(c => c.Type == "archived" && c.Value == "true"));
    }

    [Test]
    public async Task GetUsers_ExcludesArchivedUsers()
    {
        var activeUser = new ForgeUser { Id = "1", Email = "active@test.com" };
        var archivedUser = new ForgeUser { Id = "2", Email = "archived@test.com" };
        _userManager.Users.Returns(new List<ForgeUser> { activeUser, archivedUser }.AsQueryable());
        _userManager.GetRolesAsync(Arg.Any<ForgeUser>()).Returns(new List<string> { "learner" }.ToArray() as IList<string>);
        _userManager.GetClaimsAsync(Arg.Is<ForgeUser>(u => u.Id == activeUser.Id))
            .Returns(Task.FromResult<IList<Claim>>(new List<Claim>()));
        _userManager.GetClaimsAsync(Arg.Is<ForgeUser>(u => u.Id == archivedUser.Id))
            .Returns(Task.FromResult<IList<Claim>>(new List<Claim> { new Claim("archived", "true") }));

        var result = await _sut.GetUsers();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var list = ok!.Value as List<object>;
        Assert.That(list, Has.Count.EqualTo(1));
    }
}
