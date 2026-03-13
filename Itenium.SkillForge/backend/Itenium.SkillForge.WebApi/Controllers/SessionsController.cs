using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _currentUser;

    public SessionsController(AppDbContext db, ISkillForgeUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Start a focused live coaching session (FR31).
    /// Manager role only.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "manager")]
    public async Task<ActionResult<SessionDto>> StartSession([FromBody] StartSessionRequest request)
    {
        var session = new CoachingSessionEntity
        {
            CoachId = _currentUser.UserId ?? string.Empty,
            ConsultantId = request.ConsultantId,
        };

        _db.CoachingSessions.Add(session);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSessionFocus), new { sessionId = session.Id }, new SessionDto
        {
            Id = session.Id,
            CoachId = session.CoachId,
            ConsultantId = session.ConsultantId,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Notes = session.Notes,
        });
    }

    /// <summary>
    /// Get focus view for live session: pending validations + active goals (FR32).
    /// Manager role only.
    /// </summary>
    [HttpGet("{sessionId:guid}/focus")]
    [Authorize(Roles = "manager")]
    public async Task<ActionResult<SessionFocusDto>> GetSessionFocus(Guid sessionId)
    {
        var session = await _db.CoachingSessions.FindAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        if (session.CoachId != (_currentUser.UserId ?? string.Empty))
        {
            return Forbid();
        }

        var activeGoals = await _db.Goals
            .Where(g => g.ConsultantId == session.ConsultantId && g.Status == GoalStatus.Active)
            .Select(g => new GoalDto
            {
                Id = g.Id,
                SkillId = g.SkillId,
                CurrentNiveau = g.CurrentNiveau,
                TargetNiveau = g.TargetNiveau,
                Deadline = g.Deadline,
                Status = g.Status.ToString(),
            })
            .ToListAsync();

        var pendingReadinessFlags = await _db.ReadinessFlags
            .Where(rf => rf.ConsultantId == session.ConsultantId && rf.IsActive)
            .Select(rf => new ReadinessFlagDto
            {
                Id = rf.Id,
                GoalId = rf.GoalId,
                ConsultantId = rf.ConsultantId,
                RaisedAt = rf.RaisedAt,
            })
            .ToListAsync();

        return Ok(new SessionFocusDto
        {
            SessionId = sessionId,
            ConsultantId = session.ConsultantId,
            StartedAt = session.StartedAt,
            ActiveGoals = activeGoals,
            PendingReadinessFlags = pendingReadinessFlags,
        });
    }

    /// <summary>
    /// End a coaching session (FR37).
    /// Manager role only.
    /// </summary>
    [HttpPut("{sessionId:guid}/end")]
    [Authorize(Roles = "manager")]
    public async Task<ActionResult<SessionDto>> EndSession(Guid sessionId)
    {
        var session = await _db.CoachingSessions.FindAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        if (session.CoachId != (_currentUser.UserId ?? string.Empty))
        {
            return Forbid();
        }

        if (session.EndedAt.HasValue)
        {
            return Conflict(new { message = "Session already ended" });
        }

        session.EndedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new SessionDto
        {
            Id = session.Id,
            CoachId = session.CoachId,
            ConsultantId = session.ConsultantId,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Notes = session.Notes,
        });
    }

    /// <summary>
    /// Save inline session notes (FR34).
    /// Manager role only.
    /// </summary>
    [HttpPut("{sessionId:guid}/notes")]
    [Authorize(Roles = "manager")]
    public async Task<ActionResult<SessionDto>> UpdateNotes(Guid sessionId, [FromBody] UpdateNotesRequest request)
    {
        var session = await _db.CoachingSessions.FindAsync(sessionId);
        if (session == null)
        {
            return NotFound();
        }

        if (session.CoachId != (_currentUser.UserId ?? string.Empty))
        {
            return Forbid();
        }

        session.Notes = request.Notes;
        await _db.SaveChangesAsync();

        return Ok(new SessionDto
        {
            Id = session.Id,
            CoachId = session.CoachId,
            ConsultantId = session.ConsultantId,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Notes = session.Notes,
        });
    }
}

public record StartSessionRequest(string ConsultantId);

public record UpdateNotesRequest(string? Notes);

public record SessionDto
{
    public Guid Id { get; init; }
    public string CoachId { get; init; } = "";
    public string ConsultantId { get; init; } = "";
    public DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public string? Notes { get; init; }
}

public record SessionFocusDto
{
    public Guid SessionId { get; init; }
    public string ConsultantId { get; init; } = "";
    public DateTime StartedAt { get; init; }
    public IList<GoalDto> ActiveGoals { get; init; } = [];
    public IList<ReadinessFlagDto> PendingReadinessFlags { get; init; } = [];
}

public record GoalDto
{
    public Guid Id { get; init; }
    public int SkillId { get; init; }
    public int CurrentNiveau { get; init; }
    public int TargetNiveau { get; init; }
    public DateTime Deadline { get; init; }
    public string Status { get; init; } = "";
}

public record ReadinessFlagDto
{
    public Guid Id { get; init; }
    public Guid GoalId { get; init; }
    public string ConsultantId { get; init; } = "";
    public DateTime RaisedAt { get; init; }
}
