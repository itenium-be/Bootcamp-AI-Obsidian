using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Stories #25-#28: Resource library — browse, contribute, complete, rate.
/// </summary>
[TestFixture]
public class ResourcesControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private ResourcesController _sut = null!;
    private SkillEntity _skill = null!;

    [SetUp]
    public async Task Setup()
    {
        _skill = new SkillEntity { Name = "C#", Category = "Development", LevelCount = 5 };
        Db.Skills.Add(_skill);
        await Db.SaveChangesAsync();

        _user = Substitute.For<ISkillForgeUser>();
        _user.UserId.Returns("test-user-id");
        _sut = new ResourcesController(Db, _user);
    }

    // ── GET /api/resources ───────────────────────────────────────────────────

    [Test]
    public async Task GetResources_NoFilter_ReturnsAll()
    {
        Db.Resources.AddRange(
            new ResourceEntity { Title = "Intro", Url = "http://a.com", Type = ResourceType.Article, SkillId = _skill.Id, FromNiveau = 1, ToNiveau = 2, ContributedBy = "user1" },
            new ResourceEntity { Title = "Advanced", Url = "http://b.com", Type = ResourceType.Video, SkillId = _skill.Id, FromNiveau = 3, ToNiveau = 5, ContributedBy = "user2" });
        await Db.SaveChangesAsync();

        var result = await _sut.GetResources(null, null, null);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var resources = ok!.Value as List<ResourceDto>;
        Assert.That(resources, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetResources_FilterBySkillId_ReturnsMatchingOnly()
    {
        var otherSkill = new SkillEntity { Name = "Java", Category = "Development", LevelCount = 3 };
        Db.Skills.Add(otherSkill);
        await Db.SaveChangesAsync();
        Db.Resources.AddRange(
            new ResourceEntity { Title = "C# Book", Url = "http://a.com", Type = ResourceType.Book, SkillId = _skill.Id, FromNiveau = 1, ToNiveau = 3, ContributedBy = "u1" },
            new ResourceEntity { Title = "Java Book", Url = "http://b.com", Type = ResourceType.Book, SkillId = otherSkill.Id, FromNiveau = 1, ToNiveau = 3, ContributedBy = "u1" });
        await Db.SaveChangesAsync();

        var result = await _sut.GetResources(_skill.Id, null, null);

        var ok = result.Result as OkObjectResult;
        var resources = ok!.Value as List<ResourceDto>;
        Assert.That(resources, Has.Count.EqualTo(1));
        Assert.That(resources![0].Title, Is.EqualTo("C# Book"));
    }

    [Test]
    public async Task GetResources_FilterByNiveauRange_ReturnsOverlapping()
    {
        Db.Resources.AddRange(
            new ResourceEntity { Title = "Beginner", Url = "http://a.com", Type = ResourceType.Article, SkillId = _skill.Id, FromNiveau = 1, ToNiveau = 2, ContributedBy = "u1" },
            new ResourceEntity { Title = "Mid", Url = "http://b.com", Type = ResourceType.Article, SkillId = _skill.Id, FromNiveau = 2, ToNiveau = 4, ContributedBy = "u1" },
            new ResourceEntity { Title = "Expert", Url = "http://c.com", Type = ResourceType.Article, SkillId = _skill.Id, FromNiveau = 4, ToNiveau = 5, ContributedBy = "u1" });
        await Db.SaveChangesAsync();

        // Filter: fromNiveau=2, toNiveau=3 should match Beginner (toNiveau≥2) and Mid (fromNiveau≤3)
        var result = await _sut.GetResources(null, 2, 3);

        var ok = result.Result as OkObjectResult;
        var resources = ok!.Value as List<ResourceDto>;
        Assert.That(resources, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetResources_WhenEmpty_ReturnsEmptyList()
    {
        var result = await _sut.GetResources(null, null, null);

        var ok = result.Result as OkObjectResult;
        var resources = ok!.Value as List<ResourceDto>;
        Assert.That(resources, Is.Empty);
    }

    // ── GET /api/resources/{id} ──────────────────────────────────────────────

    [Test]
    public async Task GetResource_WhenExists_ReturnsResource()
    {
        var resource = new ResourceEntity { Title = "My Resource", Url = "http://x.com", Type = ResourceType.Course, SkillId = _skill.Id, FromNiveau = 1, ToNiveau = 3, ContributedBy = "u1" };
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        var result = await _sut.GetResource(resource.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dto = ok!.Value as ResourceDto;
        Assert.That(dto!.Title, Is.EqualTo("My Resource"));
    }

    [Test]
    public async Task GetResource_WhenNotFound_ReturnsNotFound()
    {
        var result = await _sut.GetResource(Guid.NewGuid());
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    // ── POST /api/resources ──────────────────────────────────────────────────

    [Test]
    public async Task CreateResource_ValidRequest_ReturnsCreated()
    {
        var request = new CreateResourceRequest("Clean Code", "http://cleancode.com", ResourceType.Book, _skill.Id, 2, 4);

        var result = await _sut.CreateResource(request);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var dto = created!.Value as ResourceDto;
        Assert.That(dto!.Title, Is.EqualTo("Clean Code"));
        Assert.That(dto.Type, Is.EqualTo("Book"));
        Assert.That(dto.ContributedBy, Is.EqualTo("test-user-id"));
    }

    [Test]
    public async Task CreateResource_SetsContributedByFromCurrentUser()
    {
        _user.UserId.Returns("authenticated-user-42");
        var request = new CreateResourceRequest("Title", "http://x.com", ResourceType.Article, _skill.Id, 1, 2);

        var result = await _sut.CreateResource(request);

        var created = result.Result as CreatedAtActionResult;
        var dto = created!.Value as ResourceDto;
        Assert.That(dto!.ContributedBy, Is.EqualTo("authenticated-user-42"));
    }

    [Test]
    public async Task CreateResource_PersistsToDatabase()
    {
        var request = new CreateResourceRequest("Persisted", "http://p.com", ResourceType.Video, _skill.Id, 1, 3);

        await _sut.CreateResource(request);

        var saved = Db.Resources.FirstOrDefault(r => r.Title == "Persisted");
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Url, Is.EqualTo("http://p.com"));
    }

    // ── POST /api/resources/{id}/complete ────────────────────────────────────

    [Test]
    public async Task CompleteResource_ValidGoal_ReturnsOk()
    {
        var resource = new ResourceEntity { Title = "Book", Url = "http://b.com", Type = ResourceType.Book, SkillId = _skill.Id, FromNiveau = 1, ToNiveau = 3, ContributedBy = "u1" };
        Db.Resources.Add(resource);
        var goal = new GoalEntity { ConsultantId = "test-user-id", CoachId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(3) };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        var result = await _sut.CompleteResource(resource.Id, new CompleteResourceRequest(goal.Id));

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dto = ok!.Value as ResourceCompletionDto;
        Assert.That(dto!.ResourceId, Is.EqualTo(resource.Id));
        Assert.That(dto.GoalId, Is.EqualTo(goal.Id));
    }

    [Test]
    public async Task CompleteResource_WhenResourceNotFound_ReturnsNotFound()
    {
        var result = await _sut.CompleteResource(Guid.NewGuid(), new CompleteResourceRequest(Guid.NewGuid()));
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task CompleteResource_WhenGoalBelongsToOtherConsultant_ReturnsForbid()
    {
        var resource = new ResourceEntity { Title = "X", Url = "http://x.com", Type = ResourceType.Article, SkillId = _skill.Id, FromNiveau = 1, ToNiveau = 2, ContributedBy = "u" };
        Db.Resources.Add(resource);
        var goal = new GoalEntity { ConsultantId = "other-user", CoachId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1) };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        _user.UserId.Returns("current-user");
        var result = await _sut.CompleteResource(resource.Id, new CompleteResourceRequest(goal.Id));

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    // ── POST /api/resources/{id}/rate ─────────────────────────────────────────

    [Test]
    public async Task RateResource_ThumbsUp_IncrementsCount()
    {
        var resource = new ResourceEntity { Title = "Rate Me", Url = "http://r.com", Type = ResourceType.Article, SkillId = _skill.Id, FromNiveau = 1, ToNiveau = 2, ContributedBy = "u1" };
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        var result = await _sut.RateResource(resource.Id, new RateResourceRequest("up"));

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);

        var updated = await Db.Resources.FindAsync(resource.Id);
        Assert.That(updated!.ThumbsUp, Is.EqualTo(1));
        Assert.That(updated.ThumbsDown, Is.EqualTo(0));
    }

    [Test]
    public async Task RateResource_ThumbsDown_IncrementsCount()
    {
        var resource = new ResourceEntity { Title = "Rate Me", Url = "http://r.com", Type = ResourceType.Article, SkillId = _skill.Id, FromNiveau = 1, ToNiveau = 2, ContributedBy = "u1" };
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        await _sut.RateResource(resource.Id, new RateResourceRequest("down"));

        var updated = await Db.Resources.FindAsync(resource.Id);
        Assert.That(updated!.ThumbsDown, Is.EqualTo(1));
        Assert.That(updated.ThumbsUp, Is.EqualTo(0));
    }

    [Test]
    public async Task RateResource_UpsertBehavior_ChangesExistingRating()
    {
        var resource = new ResourceEntity { Title = "Rate Me", Url = "http://r.com", Type = ResourceType.Article, SkillId = _skill.Id, FromNiveau = 1, ToNiveau = 2, ContributedBy = "u1" };
        Db.Resources.Add(resource);
        await Db.SaveChangesAsync();

        // First rating: thumbs up
        await _sut.RateResource(resource.Id, new RateResourceRequest("up"));
        // Change to thumbs down
        await _sut.RateResource(resource.Id, new RateResourceRequest("down"));

        var updated = await Db.Resources.FindAsync(resource.Id);
        Assert.That(updated!.ThumbsUp, Is.EqualTo(0));
        Assert.That(updated.ThumbsDown, Is.EqualTo(1));
    }

    [Test]
    public async Task RateResource_WhenResourceNotFound_ReturnsNotFound()
    {
        var result = await _sut.RateResource(Guid.NewGuid(), new RateResourceRequest("up"));
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}
