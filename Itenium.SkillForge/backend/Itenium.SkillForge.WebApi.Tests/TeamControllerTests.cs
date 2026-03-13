using System.Globalization;
using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class TeamControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private UserManager<ForgeUser> _userManager = null!;
    private TeamController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        var store = Substitute.For<IUserStore<ForgeUser>>();
        _userManager = Substitute.For<UserManager<ForgeUser>>(store, null, null, null, null, null, null, null, null);
        _sut = new TeamController(Db, _user, _userManager);
    }

    [TearDown]
    public void TearDown()
    {
        _userManager.Dispose();
    }

    [Test]
    public async Task GetUserTeams_WhenBackOffice_ReturnsAllTeams()
    {
        Db.Teams.AddRange(
            new TeamEntity { Name = "Java" },
            new TeamEntity { Name = ".NET" },
            new TeamEntity { Name = "QA" });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(true);

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Has.Count.EqualTo(3));
        Assert.That(teams.Select(t => t.Name), Contains.Item("Java"));
        Assert.That(teams.Select(t => t.Name), Contains.Item(".NET"));
        Assert.That(teams.Select(t => t.Name), Contains.Item("QA"));
    }

    [Test]
    public async Task GetUserTeams_WhenNotBackOffice_ReturnsOnlyUserTeams()
    {
        var javaTeam = new TeamEntity { Name = "Java" };
        var dotnetTeam = new TeamEntity { Name = ".NET" };
        var qaTeam = new TeamEntity { Name = "QA" };
        Db.Teams.AddRange(javaTeam, dotnetTeam, qaTeam);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { javaTeam.Id, qaTeam.Id });

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Has.Count.EqualTo(2));
        Assert.That(teams.Select(t => t.Name), Contains.Item("Java"));
        Assert.That(teams.Select(t => t.Name), Contains.Item("QA"));
        Assert.That(teams.Select(t => t.Name), Does.Not.Contain(".NET"));
    }

    [Test]
    public async Task GetUserTeams_WhenNotBackOfficeAndNoTeams_ReturnsEmpty()
    {
        Db.Teams.AddRange(
            new TeamEntity { Name = "Java" },
            new TeamEntity { Name = ".NET" });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(Array.Empty<int>());

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Is.Empty);
    }

    [Test]
    public async Task GetUserTeams_WhenUserHasNonExistentTeamId_IgnoresIt()
    {
        var javaTeam = new TeamEntity { Name = "Java" };
        Db.Teams.Add(javaTeam);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { javaTeam.Id, 999 });

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Has.Count.EqualTo(1));
        Assert.That(teams.Select(t => t.Name), Contains.Item("Java"));
    }

    // --- GetTeamMembers ---

    [Test]
    public async Task GetTeamMembers_WhenTeamNotFound_ReturnsNotFound()
    {
        var result = await _sut.GetTeamMembers(999);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetTeamMembers_ReturnsUsersWithTeamClaim()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var user1 = new ForgeUser { Id = "1", Email = "a@test.com", FirstName = "Alice", LastName = "Smith" };
        var user2 = new ForgeUser { Id = "2", Email = "b@test.com", FirstName = "Bob", LastName = "Jones" };
        _userManager.Users.Returns(new List<ForgeUser> { user1, user2 }.AsQueryable());
        _userManager.GetClaimsAsync(user1).Returns(new List<Claim> { new Claim("team", team.Id.ToString(CultureInfo.InvariantCulture)) });
        _userManager.GetClaimsAsync(user2).Returns(new List<Claim>());

        var result = await _sut.GetTeamMembers(team.Id);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetTeamMembers_ExcludesUsersNotInTeam()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var user1 = new ForgeUser { Id = "1", Email = "a@test.com", FirstName = "Alice", LastName = "Smith" };
        var user2 = new ForgeUser { Id = "2", Email = "b@test.com", FirstName = "Bob", LastName = "Jones" };
        _userManager.Users.Returns(new List<ForgeUser> { user1, user2 }.AsQueryable());
        _userManager.GetClaimsAsync(user1).Returns(new List<Claim> { new Claim("team", team.Id.ToString(CultureInfo.InvariantCulture)) });
        _userManager.GetClaimsAsync(user2).Returns(new List<Claim> { new Claim("team", "999") });

        await _userManager.Received(0).GetClaimsAsync(Arg.Any<ForgeUser>());
        var result = await _sut.GetTeamMembers(team.Id);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    // --- AddTeamMember ---

    [Test]
    public async Task AddTeamMember_WhenTeamNotFound_ReturnsNotFound()
    {
        var result = await _sut.AddTeamMember(999, new AddTeamMemberRequest("user1"));

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task AddTeamMember_WhenUserNotFound_ReturnsNotFound()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        _userManager.FindByIdAsync("user1").Returns((ForgeUser?)null);

        var result = await _sut.AddTeamMember(team.Id, new AddTeamMemberRequest("user1"));

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task AddTeamMember_WhenValidRequest_AddsClaim()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var user = new ForgeUser { Id = "1", Email = "a@test.com" };
        _userManager.FindByIdAsync("1").Returns(user);
        _userManager.GetClaimsAsync(user).Returns(new List<Claim>());
        _userManager.AddClaimAsync(user, Arg.Any<Claim>()).Returns(IdentityResult.Success);

        var result = await _sut.AddTeamMember(team.Id, new AddTeamMemberRequest("1"));

        Assert.That(result, Is.InstanceOf<OkResult>());
        await _userManager.Received(1).AddClaimAsync(user,
            Arg.Is<Claim>(c => c.Type == "team" && c.Value == team.Id.ToString(CultureInfo.InvariantCulture)));
    }

    [Test]
    public async Task AddTeamMember_WhenAlreadyMember_ReturnsConflict()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var user = new ForgeUser { Id = "1", Email = "a@test.com" };
        _userManager.FindByIdAsync("1").Returns(user);
        _userManager.GetClaimsAsync(user).Returns(new List<Claim> { new Claim("team", team.Id.ToString(CultureInfo.InvariantCulture)) });

        var result = await _sut.AddTeamMember(team.Id, new AddTeamMemberRequest("1"));

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
    }

    // --- RemoveTeamMember ---

    [Test]
    public async Task RemoveTeamMember_WhenUserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync("user1").Returns((ForgeUser?)null);

        var result = await _sut.RemoveTeamMember(1, "user1");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task RemoveTeamMember_WhenUserNotInTeam_ReturnsNotFound()
    {
        var user = new ForgeUser { Id = "1", Email = "a@test.com" };
        _userManager.FindByIdAsync("1").Returns(user);
        _userManager.GetClaimsAsync(user).Returns(new List<Claim>());

        var result = await _sut.RemoveTeamMember(1, "1");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task RemoveTeamMember_WhenValidRequest_RemovesClaim()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var claim = new Claim("team", team.Id.ToString(CultureInfo.InvariantCulture));
        var user = new ForgeUser { Id = "1", Email = "a@test.com" };
        _userManager.FindByIdAsync("1").Returns(user);
        _userManager.GetClaimsAsync(user).Returns(new List<Claim> { claim });
        _userManager.RemoveClaimAsync(user, Arg.Any<Claim>()).Returns(IdentityResult.Success);

        var result = await _sut.RemoveTeamMember(team.Id, "1");

        Assert.That(result, Is.InstanceOf<OkResult>());
        await _userManager.Received(1).RemoveClaimAsync(user,
            Arg.Is<Claim>(c => c.Type == "team" && c.Value == team.Id.ToString(CultureInfo.InvariantCulture)));
    }
}
