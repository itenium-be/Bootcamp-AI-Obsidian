using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class DashboardControllerTests : DatabaseTestBase
{
    private DashboardController _sut = null!;
    private SkillEntity _skill = null!;

    [SetUp]
    public async Task Setup()
    {
        _skill = new SkillEntity { Name = "Java", Category = "Development", LevelCount = 3 };
        Db.Skills.Add(_skill);
        await Db.SaveChangesAsync();

        _sut = new DashboardController(Db);
    }

    // --- GET /api/dashboard/coach ---

    [Test]
    public async Task GetCoachDashboard_ReturnsConsultantsForCoach()
    {
        Db.Goals.AddRange(
            new GoalEntity { ConsultantId = "learner-1", CoachId = "coach-1", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(3), Status = GoalStatus.Active },
            new GoalEntity { ConsultantId = "learner-1", CoachId = "coach-1", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(2), Status = GoalStatus.Active },
            new GoalEntity { ConsultantId = "learner-2", CoachId = "coach-1", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(1), Status = GoalStatus.Active },
            new GoalEntity { ConsultantId = "learner-3", CoachId = "coach-2", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(2), Status = GoalStatus.Active });
        await Db.SaveChangesAsync();

        var result = await _sut.GetCoachDashboard("coach-1");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var items = ok!.Value as List<CoachDashboardItem>;
        Assert.That(items, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetCoachDashboard_ReturnsActiveGoalCount()
    {
        Db.Goals.AddRange(
            new GoalEntity { ConsultantId = "learner-1", CoachId = "coach-1", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(3), Status = GoalStatus.Active },
            new GoalEntity { ConsultantId = "learner-1", CoachId = "coach-1", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(2), Status = GoalStatus.Completed });
        await Db.SaveChangesAsync();

        var result = await _sut.GetCoachDashboard("coach-1");

        var ok = result.Result as OkObjectResult;
        var items = ok!.Value as List<CoachDashboardItem>;
        Assert.That(items![0].ActiveGoalCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetCoachDashboard_WithActiveReadinessFlag_ReturnsFlagAge()
    {
        var goal = new GoalEntity { ConsultantId = "learner-1", CoachId = "coach-1", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(3), Status = GoalStatus.Active };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        var flag = new ReadinessFlagEntity { GoalId = goal.Id, ConsultantId = "learner-1", RaisedAt = DateTime.UtcNow.AddDays(-2), IsActive = true };
        Db.ReadinessFlags.Add(flag);
        await Db.SaveChangesAsync();

        var result = await _sut.GetCoachDashboard("coach-1");

        var ok = result.Result as OkObjectResult;
        var items = ok!.Value as List<CoachDashboardItem>;
        Assert.That(items![0].ReadinessFlagAgeInDays, Is.EqualTo(2).Within(1));
    }

    [Test]
    public async Task GetCoachDashboard_InactiveConsultant_SetsIsInactiveFlag()
    {
        var goal = new GoalEntity
        {
            ConsultantId = "inactive-learner",
            CoachId = "coach-1",
            SkillId = _skill.Id,
            CurrentNiveau = 1,
            TargetNiveau = 3,
            Deadline = DateTime.UtcNow.AddMonths(3),
            Status = GoalStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-25), // 25 days ago (> 3 weeks threshold)
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        var result = await _sut.GetCoachDashboard("coach-1");

        var ok = result.Result as OkObjectResult;
        var items = ok!.Value as List<CoachDashboardItem>;
        Assert.That(items![0].IsInactive, Is.True);
    }
}
