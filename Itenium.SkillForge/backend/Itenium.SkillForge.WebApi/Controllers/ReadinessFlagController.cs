using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Manages readiness flags on goals (FR18–FR20).
/// </summary>
[ApiController]
[Route("api/goals/{goalId:guid}/readiness-flag")]
[Authorize]
public class ReadinessFlagController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReadinessFlagController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Raise a readiness flag on an active goal (learner role). FR18, FR19, FR20.
    /// POST /api/goals/{goalId}/readiness-flag
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "learner")]
    public async Task<ActionResult<ReadinessFlagEntity>> RaiseFlag(
        Guid goalId,
        [FromQuery] string consultantId)
    {
        var goal = await _db.Goals.FindAsync(goalId);
        if (goal == null)
        {
            return NotFound();
        }

        if (goal.Status != GoalStatus.Active)
        {
            return BadRequest("Cannot raise a readiness flag on a non-active goal.");
        }

        // FR18: Maximum one active readiness flag per goal at a time
        var existingFlag = await _db.ReadinessFlags
            .FirstOrDefaultAsync(rf => rf.GoalId == goalId && rf.IsActive);

        if (existingFlag != null)
        {
            return BadRequest("An active readiness flag already exists for this goal.");
        }

        var flag = new ReadinessFlagEntity
        {
            GoalId = goalId,
            ConsultantId = consultantId,
            RaisedAt = DateTime.UtcNow,
            IsActive = true,
        };

        _db.ReadinessFlags.Add(flag);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(RaiseFlag), new { goalId }, flag);
    }

    /// <summary>
    /// Lower (deactivate) an active readiness flag (learner role). FR18.
    /// DELETE /api/goals/{goalId}/readiness-flag
    /// </summary>
    [HttpDelete]
    [Authorize(Roles = "learner")]
    public async Task<IActionResult> LowerFlag(
        Guid goalId,
        [FromQuery] string consultantId)
    {
        var flag = await _db.ReadinessFlags
            .FirstOrDefaultAsync(rf => rf.GoalId == goalId && rf.ConsultantId == consultantId && rf.IsActive);

        if (flag == null)
        {
            return NotFound();
        }

        flag.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
