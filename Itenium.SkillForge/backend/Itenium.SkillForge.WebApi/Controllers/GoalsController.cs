using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/goals")]
[Authorize]
public class GoalsController : ControllerBase
{
    private readonly AppDbContext _db;

    public GoalsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get goals, optionally filtered by consultant.
    /// GET /api/goals?consultantId={id}
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<GoalEntity>>> GetGoals([FromQuery] string? consultantId)
    {
        IQueryable<GoalEntity> query = _db.Goals.Include(g => g.Skill);

        if (!string.IsNullOrEmpty(consultantId))
        {
            query = query.Where(g => g.ConsultantId == consultantId);
        }

        var goals = await query.OrderBy(g => g.Deadline).ToListAsync();
        return Ok(goals);
    }

    /// <summary>
    /// Get active goals for a specific consultant.
    /// GET /api/goals/mine  (pass consultantId = current user sub)
    /// </summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<GoalEntity>>> GetMyGoals([FromQuery] string consultantId)
    {
        var goals = await _db.Goals
            .Include(g => g.Skill)
            .Where(g => g.ConsultantId == consultantId && g.Status == GoalStatus.Active)
            .OrderBy(g => g.Deadline)
            .ToListAsync();

        return Ok(goals);
    }

    /// <summary>
    /// Get a single goal by ID.
    /// GET /api/goals/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GoalEntity>> GetGoal(Guid id)
    {
        var goal = await _db.Goals
            .Include(g => g.Skill)
            .Include(g => g.ReadinessFlags)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (goal == null)
        {
            return NotFound();
        }

        return Ok(goal);
    }

    /// <summary>
    /// Create a new goal for a consultant.
    /// POST /api/goals
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<GoalEntity>> CreateGoal([FromBody] CreateGoalRequest request)
    {
        var goal = new GoalEntity
        {
            ConsultantId = request.ConsultantId,
            CoachId = request.CoachId,
            SkillId = request.SkillId,
            CurrentNiveau = request.CurrentNiveau,
            TargetNiveau = request.TargetNiveau,
            Deadline = request.Deadline,
            LinkedResourceIds = request.LinkedResourceIds,
        };

        _db.Goals.Add(goal);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGoal), new { id = goal.Id }, goal);
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public record CreateGoalRequest(
    string ConsultantId,
    string CoachId,
    int SkillId,
    int CurrentNiveau,
    int TargetNiveau,
    DateTime Deadline,
    string? LinkedResourceIds);
