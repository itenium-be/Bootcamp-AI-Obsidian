using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Story #15: Admin creates user account with role and team assignment.
/// Story #36: Admin archives a user account (soft-delete).
/// Story #37: Admin restores an archived user account.
/// Story #38: Admin views consultants without an active coach.
/// </summary>
[TestFixture]
public class UsersControllerTests : DatabaseTestBase
{
    private UserManager<ForgeUser> _userManager = null!;
    private UsersController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _userManager = CreateMockUserManager();
        _sut = new UsersController(_userManager, Db);
    }

    [TearDown]
    public void TearDown()
    {
        _userManager.Dispose();
    }

    // --- Story #15: POST /api/users ---

    [Test]
    public async Task CreateUser_WithValidRole_ReturnsCreated()
    {
        var createdUser = new ForgeUser
        {
            Id = "new-user-id",
            UserName = "sander",
            Email = "sander@test.local",
            FirstName = "Sander",
            LastName = "Dev",
        };

        _userManager.CreateAsync(Arg.Any<ForgeUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.AddToRoleAsync(Arg.Any<ForgeUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.AddClaimAsync(Arg.Any<ForgeUser>(), Arg.Any<System.Security.Claims.Claim>())
            .Returns(Task.FromResult(IdentityResult.Success));

        var request = new CreateUserRequest(
            UserName: "sander",
            Email: "sander@test.local",
            Password: "UserPassword123!",
            FirstName: "Sander",
            LastName: "Dev",
            Role: "learner",
            TeamIds: []);

        var result = await _sut.CreateUser(request);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
    }

    [Test]
    public async Task CreateUser_WithInvalidRole_ReturnsBadRequest()
    {
        var request = new CreateUserRequest(
            UserName: "sander",
            Email: "sander@test.local",
            Password: "UserPassword123!",
            FirstName: "Sander",
            LastName: "Dev",
            Role: "superadmin",
            TeamIds: []);

        var result = await _sut.CreateUser(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateUser_WithTeamIds_AssignsTeamClaims()
    {
        _userManager.CreateAsync(Arg.Any<ForgeUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.AddToRoleAsync(Arg.Any<ForgeUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.AddClaimAsync(Arg.Any<ForgeUser>(), Arg.Any<System.Security.Claims.Claim>())
            .Returns(Task.FromResult(IdentityResult.Success));

        var request = new CreateUserRequest(
            UserName: "coach1",
            Email: "coach1@test.local",
            Password: "UserPassword123!",
            FirstName: "Coach",
            LastName: "One",
            Role: "manager",
            TeamIds: [1, 2]);

        await _sut.CreateUser(request);

        // Verify AddClaimAsync was called twice (once for each team)
        await _userManager.Received(2).AddClaimAsync(
            Arg.Any<ForgeUser>(),
            Arg.Is<System.Security.Claims.Claim>(c => c.Type == "team"));
    }

    [Test]
    public async Task CreateUser_AllThreeRolesAreValid()
    {
        foreach (var role in new[] { "learner", "manager", "backoffice" })
        {
            _userManager.CreateAsync(Arg.Any<ForgeUser>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Success));
            _userManager.AddToRoleAsync(Arg.Any<ForgeUser>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Success));
            _userManager.AddClaimAsync(Arg.Any<ForgeUser>(), Arg.Any<System.Security.Claims.Claim>())
                .Returns(Task.FromResult(IdentityResult.Success));

            var request = new CreateUserRequest(
                UserName: $"user-{role}",
                Email: $"{role}@test.local",
                Password: "UserPassword123!",
                FirstName: "Test",
                LastName: "User",
                Role: role,
                TeamIds: []);

            var result = await _sut.CreateUser(request);
            Assert.That(result.Result, Is.Not.TypeOf<BadRequestObjectResult>(),
                $"Role '{role}' should be valid but was rejected.");
        }
    }

    // --- Story #36: POST /api/users/{id}/archive ---

    [Test]
    public async Task ArchiveUser_WhenExists_SetsIsArchivedAndLockout()
    {
        // Arrange: add a real user to the db
        var user = new ForgeUser
        {
            Id = "user-to-archive",
            UserName = "tobearchived",
            Email = "archive@test.local",
            FirstName = "To",
            LastName = "Archive",
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();

        _userManager.FindByIdAsync("user-to-archive")
            .Returns(Task.FromResult<ForgeUser?>(user));
        _userManager.SetLockoutEnabledAsync(user, true)
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue)
            .Returns(Task.FromResult(IdentityResult.Success));

        var result = await _sut.ArchiveUser("user-to-archive");

        Assert.That(result, Is.TypeOf<NoContentResult>());
        // Verify IsArchived shadow property was set
        var isArchived = Db.Entry(user).Property<bool>("IsArchived").CurrentValue;
        Assert.That(isArchived, Is.True);
    }

    [Test]
    public async Task ArchiveUser_WhenNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync("nonexistent").Returns(Task.FromResult<ForgeUser?>(null));

        var result = await _sut.ArchiveUser("nonexistent");

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task ArchiveUser_WhenAlreadyArchived_ReturnsConflict()
    {
        var user = new ForgeUser
        {
            Id = "already-archived",
            UserName = "alreadyarchived",
            Email = "alreadyarchived@test.local",
            FirstName = "Already",
            LastName = "Archived",
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();

        // Set archived via shadow property
        Db.Entry(user).Property("IsArchived").CurrentValue = true;
        await Db.SaveChangesAsync();

        _userManager.FindByIdAsync("already-archived")
            .Returns(Task.FromResult<ForgeUser?>(user));

        var result = await _sut.ArchiveUser("already-archived");

        Assert.That(result, Is.TypeOf<ConflictObjectResult>());
    }

    // --- Story #37: POST /api/users/{id}/restore ---

    [Test]
    public async Task RestoreUser_WhenArchived_ClearsIsArchivedAndLockout()
    {
        var user = new ForgeUser
        {
            Id = "user-to-restore",
            UserName = "toberestore",
            Email = "restore@test.local",
            FirstName = "To",
            LastName = "Restore",
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();

        Db.Entry(user).Property("IsArchived").CurrentValue = true;
        Db.Entry(user).Property("ArchivedAt").CurrentValue = DateTime.UtcNow.AddDays(-7);
        await Db.SaveChangesAsync();

        _userManager.FindByIdAsync("user-to-restore")
            .Returns(Task.FromResult<ForgeUser?>(user));
        _userManager.SetLockoutEndDateAsync(user, null)
            .Returns(Task.FromResult(IdentityResult.Success));

        var result = await _sut.RestoreUser("user-to-restore");

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var isArchived = Db.Entry(user).Property<bool>("IsArchived").CurrentValue;
        Assert.That(isArchived, Is.False);
    }

    [Test]
    public async Task RestoreUser_WhenNotArchived_ReturnsConflict()
    {
        var user = new ForgeUser
        {
            Id = "active-user",
            UserName = "activeuser",
            Email = "active@test.local",
            FirstName = "Active",
            LastName = "User",
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();

        _userManager.FindByIdAsync("active-user")
            .Returns(Task.FromResult<ForgeUser?>(user));

        var result = await _sut.RestoreUser("active-user");

        Assert.That(result, Is.TypeOf<ConflictObjectResult>());
    }

    [Test]
    public async Task RestoreUser_WhenNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync("nonexistent").Returns(Task.FromResult<ForgeUser?>(null));

        var result = await _sut.RestoreUser("nonexistent");

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    // --- Story #38: GET /api/users/orphaned-consultants ---

    [Test]
    public async Task GetOrphanedConsultants_WhenConsultantHasNoProfile_IsOrphaned()
    {
        var skill = new SkillEntity { Name = "C#", Category = "Dev", LevelCount = 3 };
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        var learner = new ForgeUser
        {
            Id = "orphaned-learner",
            UserName = "orphaned",
            Email = "orphaned@test.local",
            FirstName = "Orphaned",
            LastName = "Learner",
        };
        Db.Users.Add(learner);
        await Db.SaveChangesAsync();

        _userManager.IsInRoleAsync(Arg.Is<ForgeUser>(u => u.Id == "orphaned-learner"), "learner")
            .Returns(true);

        var result = await _sut.GetOrphanedConsultants();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var consultants = ok!.Value as List<UserResponse>;
        Assert.That(consultants!.Any(c => c.Id == "orphaned-learner"), Is.True);
    }

    [Test]
    public async Task GetOrphanedConsultants_WhenConsultantHasActiveCoach_IsNotOrphaned()
    {
        var learner = new ForgeUser
        {
            Id = "assigned-learner",
            UserName = "assigned",
            Email = "assigned@test.local",
            FirstName = "Assigned",
            LastName = "Learner",
        };
        Db.Users.Add(learner);
        await Db.SaveChangesAsync();

        // Assign profile (has active coach)
        Db.ConsultantProfiles.Add(new ConsultantProfileEntity
        {
            UserId = "assigned-learner",
            AssignedBy = "active-coach-id",
        });
        await Db.SaveChangesAsync();

        _userManager.IsInRoleAsync(Arg.Is<ForgeUser>(u => u.Id == "assigned-learner"), "learner")
            .Returns(true);

        var result = await _sut.GetOrphanedConsultants();

        var ok = result.Result as OkObjectResult;
        var consultants = ok!.Value as List<UserResponse>;
        Assert.That(consultants!.All(c => c.Id != "assigned-learner"), Is.True);
    }

    private static UserManager<ForgeUser> CreateMockUserManager()
    {
        var store = Substitute.For<IUserStore<ForgeUser>>();
        var options = Substitute.For<IOptions<IdentityOptions>>();
        options.Value.Returns(new IdentityOptions());
        var passwordHasher = Substitute.For<IPasswordHasher<ForgeUser>>();
        var userValidators = Array.Empty<IUserValidator<ForgeUser>>();
        var passwordValidators = Array.Empty<IPasswordValidator<ForgeUser>>();
        var keyNormalizer = Substitute.For<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        var services = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<UserManager<ForgeUser>>>();

        return Substitute.ForPartsOf<UserManager<ForgeUser>>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger);
    }
}
