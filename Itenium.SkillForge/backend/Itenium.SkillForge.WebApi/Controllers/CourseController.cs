using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Manages courses in the SkillForge LMS.
/// BackOffice and managers can create, update, and delete courses.
/// All authenticated users can read courses.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CourseController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    /// <summary>
    /// Initializes a new instance of <see cref="CourseController"/>.
    /// </summary>
    /// <param name="db">The application database context.</param>
    /// <param name="user">The current authenticated user abstraction.</param>
    public CourseController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get all courses (requires ReadCourse capability).
    /// BackOffice sees all; managers see all; learners see all.
    /// </summary>
    /// <returns>A list of all courses.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CourseEntity>>> GetCourses()
    {
        var courses = await _db.Courses.ToListAsync();
        return Ok(courses);
    }

    /// <summary>
    /// Get a course by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the course.</param>
    /// <returns>The course with the specified ID.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseEntity>> GetCourse(int id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound();
        }

        return Ok(course);
    }

    /// <summary>
    /// Create a new course (requires ManageCourse capability).
    /// </summary>
    /// <param name="request">The course creation request containing name, description, category, and level.</param>
    /// <returns>The newly created course.</returns>
    /// <remarks>Only BackOffice users and managers with team access can create courses.</remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CourseEntity>> CreateCourse([FromBody] CreateCourseRequest request)
    {
        if (!_user.IsBackOffice && _user.Teams.Count == 0)
        {
            return Forbid();
        }

        var course = new CourseEntity
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Level = request.Level
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
    }

    /// <summary>
    /// Update an existing course (requires ManageCourse capability).
    /// </summary>
    /// <param name="id">The unique identifier of the course to update.</param>
    /// <param name="request">The update request containing the new course details.</param>
    /// <returns>The updated course.</returns>
    /// <remarks>Only BackOffice users and managers with team access can update courses.</remarks>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseEntity>> UpdateCourse(int id, [FromBody] UpdateCourseRequest request)
    {
        if (!_user.IsBackOffice && _user.Teams.Count == 0)
        {
            return Forbid();
        }

        var course = await _db.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound();
        }

        course.Name = request.Name;
        course.Description = request.Description;
        course.Category = request.Category;
        course.Level = request.Level;

        await _db.SaveChangesAsync();

        return Ok(course);
    }

    /// <summary>
    /// Delete a course (requires ManageCourse capability).
    /// </summary>
    /// <param name="id">The unique identifier of the course to delete.</param>
    /// <returns>No content on success.</returns>
    /// <remarks>Only BackOffice users and managers with team access can delete courses.</remarks>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteCourse(int id)
    {
        if (!_user.IsBackOffice && _user.Teams.Count == 0)
        {
            return Forbid();
        }

        var course = await _db.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound();
        }

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
