using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Stories #31-#33: Live Session — start, focus view, end, notes.
/// </summary>
[TestFixture]
public class SessionsControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private SessionsController _sut = null!;
    private SkillEntity _skill = null!;

    [SetUp]
    public async Task Setup()
    {
        _skill = new SkillEntity { Name = "Clean Code", Category = "Development", LevelCount = 3 };
        Db.Skills.Add(_skill);
        await Db.SaveChangesAsync();

        _user = Substitute.For<ISkillForgeUser>();
        _user.Id.Returns("coach-id");
        _sut = new SessionsController(Db, _user);
    }

    // ── POST /api/sessions ───────────────────────────────────────────────────

    [Test]
    public async Task StartSession_ValidRequest_ReturnsCreated()
    {
        var result = await _sut.StartSession(new StartSessionRequest("consultant-1"));

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var dto = created!.Value as SessionDto;
        Assert.That(dto!.CoachId, Is.EqualTo("coach-id"));
        Assert.That(dto.ConsultantId, Is.EqualTo("consultant-1"));
        Assert.That(dto.EndedAt, Is.Null);
    }

    [Test]
    public async Task StartSession_PersistsToDatabase()
    {
        await _sut.StartSession(new StartSessionRequest("lea-consultant"));

        var saved = Db.CoachingSessions.FirstOrDefault(s => s.ConsultantId == "lea-consultant");
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.CoachId, Is.EqualTo("coach-id"));
    }

    // ── GET /api/sessions/{sessionId}/focus ──────────────────────────────────

    [Test]
    public async Task GetSessionFocus_ReturnsActiveGoalsAndReadinessFlags()
    {
        var session = new CoachingSessionEntity { CoachId = "coach-id", ConsultantId = "consultant-1" };
        Db.CoachingSessions.Add(session);

        var goal1 = new GoalEntity { ConsultantId = "consultant-1", CoachId = "coach-id", SkillId = _skill.Id, CurrentNiveau = 1, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(3), Status = GoalStatus.Active };
        var goal2 = new GoalEntity { ConsultantId = "consultant-1", CoachId = "coach-id", SkillId = _skill.Id, CurrentNiveau = 2, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(1), Status = GoalStatus.Completed };
        Db.Goals.AddRange(goal1, goal2);

        var flag = new ReadinessFlagEntity { GoalId = goal1.Id, ConsultantId = "consultant-1", IsActive = true };
        Db.ReadinessFlags.Add(flag);
        await Db.SaveChangesAsync();

        var result = await _sut.GetSessionFocus(session.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var focus = ok!.Value as SessionFocusDto;
        Assert.That(focus!.ActiveGoals, Has.Count.EqualTo(1));
        Assert.That(focus.ActiveGoals[0].Status, Is.EqualTo("Active"));
        Assert.That(focus.PendingReadinessFlags, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetSessionFocus_WhenSessionNotFound_ReturnsNotFound()
    {
        var result = await _sut.GetSessionFocus(Guid.NewGuid());
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetSessionFocus_WhenCoachMismatch_ReturnsForbid()
    {
        var session = new CoachingSessionEntity { CoachId = "other-coach", ConsultantId = "consultant-1" };
        Db.CoachingSessions.Add(session);
        await Db.SaveChangesAsync();

        _user.Id.Returns("coach-id");
        var result = await _sut.GetSessionFocus(session.Id);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    // ── PUT /api/sessions/{sessionId}/end ────────────────────────────────────

    [Test]
    public async Task EndSession_ValidSession_SetsEndedAt()
    {
        var session = new CoachingSessionEntity { CoachId = "coach-id", ConsultantId = "consultant-1" };
        Db.CoachingSessions.Add(session);
        await Db.SaveChangesAsync();

        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = await _sut.EndSession(session.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dto = ok!.Value as SessionDto;
        Assert.That(dto!.EndedAt, Is.Not.Null);
        Assert.That(dto.EndedAt!.Value, Is.GreaterThan(before));
    }

    [Test]
    public async Task EndSession_AlreadyEnded_ReturnsConflict()
    {
        var session = new CoachingSessionEntity { CoachId = "coach-id", ConsultantId = "consultant-1", EndedAt = DateTime.UtcNow };
        Db.CoachingSessions.Add(session);
        await Db.SaveChangesAsync();

        var result = await _sut.EndSession(session.Id);
        Assert.That(result.Result, Is.TypeOf<ConflictObjectResult>());
    }

    [Test]
    public async Task EndSession_WhenNotFound_ReturnsNotFound()
    {
        var result = await _sut.EndSession(Guid.NewGuid());
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    // ── PUT /api/sessions/{sessionId}/notes ──────────────────────────────────

    [Test]
    public async Task UpdateNotes_ValidSession_SavesNotes()
    {
        var session = new CoachingSessionEntity { CoachId = "coach-id", ConsultantId = "consultant-1" };
        Db.CoachingSessions.Add(session);
        await Db.SaveChangesAsync();

        var result = await _sut.UpdateNotes(session.Id, new UpdateNotesRequest("Strong grasp of naming and functions."));

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dto = ok!.Value as SessionDto;
        Assert.That(dto!.Notes, Is.EqualTo("Strong grasp of naming and functions."));

        var saved = await Db.CoachingSessions.FindAsync(session.Id);
        Assert.That(saved!.Notes, Is.EqualTo("Strong grasp of naming and functions."));
    }

    [Test]
    public async Task UpdateNotes_WhenSessionNotFound_ReturnsNotFound()
    {
        var result = await _sut.UpdateNotes(Guid.NewGuid(), new UpdateNotesRequest("notes"));
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task UpdateNotes_WhenCoachMismatch_ReturnsForbid()
    {
        var session = new CoachingSessionEntity { CoachId = "other-coach", ConsultantId = "consultant-1" };
        Db.CoachingSessions.Add(session);
        await Db.SaveChangesAsync();

        _user.Id.Returns("coach-id");
        var result = await _sut.UpdateNotes(session.Id, new UpdateNotesRequest("notes"));

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }
}
