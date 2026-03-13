using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class ReadinessFlagControllerTests : DatabaseTestBase
{
    private ReadinessFlagController _sut = null!;
    private SkillEntity _skill = null!;
    private GoalEntity _goal = null!;

    [SetUp]
    public async Task Setup()
    {
        _skill = new SkillEntity { Name = "C#", Category = "Development", LevelCount = 3 };
        Db.Skills.Add(_skill);
        await Db.SaveChangesAsync();

        _goal = new GoalEntity
        {
            ConsultantId = "consultant-1",
            CoachId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 3,
            Deadline = DateTime.UtcNow.AddMonths(3),
            Status = GoalStatus.Active,
        };
        Db.Goals.Add(_goal);
        await Db.SaveChangesAsync();

        _sut = new ReadinessFlagController(Db);
    }

    // --- POST /api/goals/{goalId}/readiness-flag ---

    [Test]
    public async Task RaiseFlag_OnActiveGoal_ReturnsCreated()
    {
        var result = await _sut.RaiseFlag(_goal.Id, "consultant-1");

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var flag = created!.Value as ReadinessFlagEntity;
        Assert.That(flag!.GoalId, Is.EqualTo(_goal.Id));
        Assert.That(flag.IsActive, Is.True);
        Assert.That(flag.RaisedAt, Is.EqualTo(DateTime.UtcNow).Within(5).Seconds);
    }

    [Test]
    public async Task RaiseFlag_WhenFlagAlreadyActive_ReturnsBadRequest()
    {
        // First flag
        await _sut.RaiseFlag(_goal.Id, "consultant-1");

        // Second flag on same goal
        var result = await _sut.RaiseFlag(_goal.Id, "consultant-1");

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RaiseFlag_WhenGoalNotFound_ReturnsNotFound()
    {
        var result = await _sut.RaiseFlag(Guid.NewGuid(), "consultant-1");

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task RaiseFlag_WhenGoalIsCompleted_ReturnsBadRequest()
    {
        _goal.Status = GoalStatus.Completed;
        await Db.SaveChangesAsync();

        var result = await _sut.RaiseFlag(_goal.Id, "consultant-1");

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    // --- DELETE /api/goals/{goalId}/readiness-flag ---

    [Test]
    public async Task LowerFlag_WhenFlagExists_DeactivatesFlag()
    {
        var flag = new ReadinessFlagEntity { GoalId = _goal.Id, ConsultantId = "consultant-1", IsActive = true };
        Db.ReadinessFlags.Add(flag);
        await Db.SaveChangesAsync();

        var result = await _sut.LowerFlag(_goal.Id, "consultant-1");

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var deactivated = await Db.ReadinessFlags.FindAsync(flag.Id);
        Assert.That(deactivated!.IsActive, Is.False);
    }

    [Test]
    public async Task LowerFlag_WhenNoActiveFlag_ReturnsNotFound()
    {
        var result = await _sut.LowerFlag(_goal.Id, "consultant-1");

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}
