using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class ConsultantActivityControllerTests : DatabaseTestBase
{
    private ConsultantActivityController _sut = null!;
    private SkillEntity _skill = null!;

    [SetUp]
    public async Task Setup()
    {
        _skill = new SkillEntity { Name = "C#", Category = "Development", LevelCount = 3 };
        Db.Skills.Add(_skill);
        await Db.SaveChangesAsync();

        _sut = new ConsultantActivityController(Db);
    }

    // --- GET /api/consultants/{id}/activity ---

    [Test]
    public async Task GetActivity_ReturnsChronologicalFeed()
    {
        var goal1 = new GoalEntity
        {
            ConsultantId = "learner-1",
            CoachId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 3,
            Deadline = DateTime.UtcNow.AddMonths(3),
            Status = GoalStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
        };
        var goal2 = new GoalEntity
        {
            ConsultantId = "learner-1",
            CoachId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 2,
            TargetNiveau = 3,
            Deadline = DateTime.UtcNow.AddMonths(2),
            Status = GoalStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
        };
        Db.Goals.AddRange(goal1, goal2);
        await Db.SaveChangesAsync();

        var flag = new ReadinessFlagEntity
        {
            GoalId = goal1.Id,
            ConsultantId = "learner-1",
            RaisedAt = DateTime.UtcNow.AddDays(-3),
            IsActive = true,
        };
        Db.ReadinessFlags.Add(flag);
        await Db.SaveChangesAsync();

        var result = await _sut.GetActivity("learner-1");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var items = ok!.Value as List<ActivityFeedItem>;
        Assert.That(items, Has.Count.EqualTo(3));

        // Should be in descending order (newest first)
        Assert.That(items![0].OccurredAt, Is.GreaterThan(items[1].OccurredAt));
        Assert.That(items[1].OccurredAt, Is.GreaterThan(items[2].OccurredAt));
    }

    [Test]
    public async Task GetActivity_IncludesGoalCreatedEvents()
    {
        var goal = new GoalEntity
        {
            ConsultantId = "learner-1",
            CoachId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 2,
            Deadline = DateTime.UtcNow.AddMonths(1),
            Status = GoalStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        var result = await _sut.GetActivity("learner-1");

        var ok = result.Result as OkObjectResult;
        var items = ok!.Value as List<ActivityFeedItem>;
        var goalEvent = items!.FirstOrDefault(i => i.Type == ActivityType.GoalCreated);
        Assert.That(goalEvent, Is.Not.Null);
    }

    [Test]
    public async Task GetActivity_IncludesReadinessFlagEvents()
    {
        var goal = new GoalEntity
        {
            ConsultantId = "learner-1",
            CoachId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 2,
            Deadline = DateTime.UtcNow.AddMonths(1),
            Status = GoalStatus.Active,
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        Db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = goal.Id, ConsultantId = "learner-1", RaisedAt = DateTime.UtcNow.AddDays(-1) });
        await Db.SaveChangesAsync();

        var result = await _sut.GetActivity("learner-1");

        var ok = result.Result as OkObjectResult;
        var items = ok!.Value as List<ActivityFeedItem>;
        Assert.That(items!.Any(i => i.Type == ActivityType.ReadinessFlagRaised), Is.True);
    }

    [Test]
    public async Task GetActivity_OnlyReturnsDataForRequestedConsultant()
    {
        Db.Goals.AddRange(
            new GoalEntity { ConsultantId = "learner-1", CoachId = "coach-1", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1) },
            new GoalEntity { ConsultantId = "learner-2", CoachId = "coach-1", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(2) });
        await Db.SaveChangesAsync();

        var result = await _sut.GetActivity("learner-1");

        var ok = result.Result as OkObjectResult;
        var items = ok!.Value as List<ActivityFeedItem>;
        Assert.That(items!.All(i => i.ConsultantId == "learner-1"), Is.True);
    }
}
