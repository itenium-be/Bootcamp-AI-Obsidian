using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class CourseTeamControllerTests : DatabaseTestBase
{
    private CourseTeamController _sut = null!;
    private ISkillForgeUser _user = null!;

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _user.IsBackOffice.Returns(true);
        _user.Teams.Returns(new List<int>());
        _sut = new CourseTeamController(Db, _user);
    }

    private async Task<CourseEntity> CreateCourse(string name = "Test Course")
    {
        var course = new CourseEntity { Name = name };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        return course;
    }

    private async Task<TeamEntity> CreateTeam(string name = "Test Team")
    {
        var team = new TeamEntity { Name = name };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        return team;
    }

    [Test]
    public async Task GetTeamsForCourse_WhenCourseNotFound_ReturnsNotFound()
    {
        var result = await _sut.GetTeamsForCourse(999);
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetTeamsForCourse_WhenNoAssignments_ReturnsEmptyList()
    {
        var course = await CreateCourse();

        var result = await _sut.GetTeamsForCourse(course.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var teams = ok!.Value as List<TeamEntity>;
        Assert.That(teams, Is.Empty);
    }

    [Test]
    public async Task GetTeamsForCourse_WhenAssigned_ReturnsTeams()
    {
        var course = await CreateCourse();
        var team1 = await CreateTeam("Java");
        var team2 = await CreateTeam(".NET");
        Db.CourseTeams.AddRange(
            new CourseTeamEntity { CourseId = course.Id, TeamId = team1.Id },
            new CourseTeamEntity { CourseId = course.Id, TeamId = team2.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.GetTeamsForCourse(course.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var teams = ok!.Value as List<TeamEntity>;
        Assert.That(teams, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetTeamsForCourse_AsLearnerWithNoTeams_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new List<int>());
        var course = await CreateCourse();

        var result = await _sut.GetTeamsForCourse(course.Id);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task AssignTeam_WhenValidCourseAndTeam_ReturnsCreated()
    {
        var course = await CreateCourse();
        var team = await CreateTeam();

        var result = await _sut.AssignTeam(course.Id, team.Id);

        var createdResult = result.Result as CreatedAtActionResult;
        Assert.That(createdResult, Is.Not.Null);
        var assignment = createdResult!.Value as CourseTeamEntity;
        Assert.That(assignment!.CourseId, Is.EqualTo(course.Id));
        Assert.That(assignment.TeamId, Is.EqualTo(team.Id));
    }

    [Test]
    public async Task AssignTeam_WhenCourseNotFound_ReturnsNotFound()
    {
        var team = await CreateTeam();

        var result = await _sut.AssignTeam(999, team.Id);

        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task AssignTeam_WhenTeamNotFound_ReturnsNotFound()
    {
        var course = await CreateCourse();

        var result = await _sut.AssignTeam(course.Id, 999);

        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task AssignTeam_WhenAlreadyAssigned_ReturnsConflict()
    {
        var course = await CreateCourse();
        var team = await CreateTeam();
        Db.CourseTeams.Add(new CourseTeamEntity { CourseId = course.Id, TeamId = team.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.AssignTeam(course.Id, team.Id);

        Assert.That(result.Result, Is.TypeOf<ConflictObjectResult>());
    }

    [Test]
    public async Task AssignTeam_AsNonBackOffice_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);
        var course = await CreateCourse();
        var team = await CreateTeam();

        var result = await _sut.AssignTeam(course.Id, team.Id);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task RemoveTeam_WhenExists_ReturnsNoContent()
    {
        var course = await CreateCourse();
        var team = await CreateTeam();
        Db.CourseTeams.Add(new CourseTeamEntity { CourseId = course.Id, TeamId = team.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.RemoveTeam(course.Id, team.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var assignment = await Db.CourseTeams.FindAsync(course.Id);
        Assert.That(assignment, Is.Null);
    }

    [Test]
    public async Task RemoveTeam_WhenNotExists_ReturnsNotFound()
    {
        var result = await _sut.RemoveTeam(1, 999);
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task RemoveTeam_AsNonBackOffice_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);

        var result = await _sut.RemoveTeam(1, 1);

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }
}
