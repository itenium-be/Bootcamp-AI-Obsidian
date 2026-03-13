using System.Reflection;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Tests for Issue #11: Consultant login with own-data access scope.
///
/// FR1: Consultant can only access own data — admin routes blocked by [Authorize(Roles = "backoffice")].
/// FR4: Cannot write skill validations (403) — ValidationController requires "manager" role.
///      This is enforced via [Authorize(Roles = "manager")] on ValidationController (pending implementation in issue #14).
/// </summary>
[TestFixture]
public class ConsultantAccessTests
{
    [Test]
    public void UserController_BlocksNonBackofficeUsers_LearnerCannotAccess()
    {
        // FR1: Learner (consultant) cannot access backoffice admin endpoints.
        // The [Authorize(Roles = "backoffice")] attribute on UserController
        // ensures that only backoffice users can call /api/user endpoints.
        var type = typeof(UserController);
        var attr = type.GetCustomAttribute<AuthorizeAttribute>();
        Assert.That(attr, Is.Not.Null, "UserController must require authorization");
        Assert.That(attr!.Roles, Does.Contain("backoffice"),
            "UserController must restrict access to backoffice role only");
    }

    [Test]
    public void UserController_OnlyAllowsBackofficeRole_NotLearnerOrManager()
    {
        // The allowed roles must be exactly "backoffice", not including "learner" or "manager".
        var type = typeof(UserController);
        var attr = type.GetCustomAttribute<AuthorizeAttribute>();
        Assert.That(attr!.Roles, Is.EqualTo("backoffice"),
            "UserController must only allow backoffice role, not learner or manager");
    }

    // FR4 Note: Skill validation writes are blocked at API level for non-manager roles.
    // ValidationController uses [Authorize(Roles = "manager")], which means:
    //   - Learner (consultant) receives 403 when attempting POST /api/validation
    //   - BackOffice admin receives 403 when attempting POST /api/validation
    // This will be tested in ValidationControllerTests once issue #14 is implemented.
}
