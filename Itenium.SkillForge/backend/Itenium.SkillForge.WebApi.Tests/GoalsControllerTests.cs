using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class GoalsControllerTests : DatabaseTestBase
{
    private GoalsController _sut = null!;
    private SkillEntity _skill = null!;

    [SetUp]
    public async Task Setup()
    {
        _skill = new SkillEntity { Name = "C#", Category = "Development", LevelCount = 3 };
        Db.Skills.Add(_skill);
        await Db.SaveChangesAsync();

        _sut = new GoalsController(Db);
    }

    // --- POST /api/goals ---

    [Test]
    public async Task CreateGoal_ValidRequest_ReturnsCreated()
    {
        var request = new CreateGoalRequest(
            ConsultantId: "consultant-1",
            CoachId: "coach-1",
            SkillId: _skill.Id,
            CurrentNiveau: 1,
            TargetNiveau: 3,
            Deadline: DateTime.UtcNow.AddMonths(3),
            LinkedResourceIds: null);

        var result = await _sut.CreateGoal(request);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var goal = created!.Value as GoalEntity;
        Assert.That(goal!.ConsultantId, Is.EqualTo("consultant-1"));
        Assert.That(goal.SkillId, Is.EqualTo(_skill.Id));
        Assert.That(goal.Status, Is.EqualTo(GoalStatus.Active));
    }

    [Test]
    public async Task CreateGoal_PersistsToDatabase()
    {
        var request = new CreateGoalRequest(
            ConsultantId: "consultant-2",
            CoachId: "coach-1",
            SkillId: _skill.Id,
            CurrentNiveau: 1,
            TargetNiveau: 2,
            Deadline: DateTime.UtcNow.AddMonths(1),
            LinkedResourceIds: "res-1,res-2");

        await _sut.CreateGoal(request);

        var saved = Db.Goals.FirstOrDefault(g => g.ConsultantId == "consultant-2");
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.LinkedResourceIds, Is.EqualTo("res-1,res-2"));
    }

    // --- GET /api/goals?consultantId={id} ---

    [Test]
    public async Task GetGoals_ByConsultantId_ReturnsMatchingGoals()
    {
        Db.Goals.AddRange(
            new GoalEntity { ConsultantId = "c1", CoachId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1) },
            new GoalEntity { ConsultantId = "c1", CoachId = "coach", SkillId = _skill.Id, CurrentNiveau = 2, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(2) },
            new GoalEntity { ConsultantId = "c2", CoachId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(3) });
        await Db.SaveChangesAsync();

        var result = await _sut.GetGoals("c1");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var goals = ok!.Value as List<GoalEntity>;
        Assert.That(goals, Has.Count.EqualTo(2));
        Assert.That(goals!.All(g => g.ConsultantId == "c1"), Is.True);
    }

    [Test]
    public async Task GetGoals_WhenNoConsultantId_ReturnsAllGoals()
    {
        Db.Goals.AddRange(
            new GoalEntity { ConsultantId = "c1", CoachId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1) },
            new GoalEntity { ConsultantId = "c2", CoachId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(2) });
        await Db.SaveChangesAsync();

        var result = await _sut.GetGoals(null);

        var ok = result.Result as OkObjectResult;
        var goals = ok!.Value as List<GoalEntity>;
        Assert.That(goals, Has.Count.EqualTo(2));
    }

    // --- GET /api/goals/mine ---

    [Test]
    public async Task GetMyGoals_ReturnsActiveGoalsForCurrentUser()
    {
        const string consultantId = "current-user-id";
        Db.Goals.AddRange(
            new GoalEntity { ConsultantId = consultantId, CoachId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(2), Status = GoalStatus.Active },
            new GoalEntity { ConsultantId = consultantId, CoachId = "coach", SkillId = _skill.Id, CurrentNiveau = 2, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(1), Status = GoalStatus.Completed },
            new GoalEntity { ConsultantId = "other-user", CoachId = "coach", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1), Status = GoalStatus.Active });
        await Db.SaveChangesAsync();

        var result = await _sut.GetMyGoals(consultantId);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var goals = ok!.Value as List<GoalEntity>;
        Assert.That(goals, Has.Count.EqualTo(1));
        Assert.That(goals![0].ConsultantId, Is.EqualTo(consultantId));
        Assert.That(goals[0].Status, Is.EqualTo(GoalStatus.Active));
    }
}
