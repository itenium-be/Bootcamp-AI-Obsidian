using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Tests for ValidationController.
/// FR4: POST /validations returns 403 for learner and backoffice roles (only manager allowed).
/// FR36: Every validation records validatedBy (coach user ID) + validatedAt timestamp.
/// FR36: ValidatedBy and ValidatedAt fields are immutable once written.
/// </summary>
[TestFixture]
public class ValidationControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _currentUser = null!;
    private ValidationController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _currentUser = Substitute.For<ISkillForgeUser>();
        _currentUser.UserId.Returns("coach-user-id");
        _sut = new ValidationController(Db, _currentUser);
    }

    // --- Authorization (FR4) ---

    [Test]
    public void ValidationController_RequiresManagerRole()
    {
        // FR4: Restriction enforced server-side — only manager role allowed
        var authorizeAttr = typeof(ValidationController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.That(authorizeAttr, Is.Not.Null, "ValidationController must have [Authorize] attribute");
        Assert.That(authorizeAttr!.Roles, Is.EqualTo("manager"),
            "Only manager role should be allowed (learner and backoffice must get 403)");
    }

    // --- Happy path (FR36) ---

    [Test]
    public async Task CreateValidation_WithManagerRole_ReturnsCreated()
    {
        var request = new CreateValidationRequest("C#", "learner-user-id", "intermediate");

        var result = await _sut.CreateValidation(request);

        Assert.That(result, Is.InstanceOf<CreatedResult>());
    }

    [Test]
    public async Task CreateValidation_SetsValidatedByFromCurrentUser()
    {
        // FR36: validatedBy must come from the authenticated user's identity, not from the request
        _currentUser.UserId.Returns("specific-coach-id");
        var request = new CreateValidationRequest("Java", "learner-1", "beginner");

        var result = await _sut.CreateValidation(request);

        var createdResult = result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null);
        var entity = createdResult!.Value as SkillValidationEntity;
        Assert.That(entity!.ValidatedBy, Is.EqualTo("specific-coach-id"));
    }

    [Test]
    public async Task CreateValidation_SetsValidatedAtToCurrentTime()
    {
        // FR36: validatedAt timestamp must be set to the current UTC time
        var before = DateTime.UtcNow.AddSeconds(-1);
        var request = new CreateValidationRequest("Python", "learner-2", "advanced");

        var result = await _sut.CreateValidation(request);

        var createdResult = result as CreatedResult;
        Assert.That(createdResult, Is.Not.Null);
        var entity = createdResult!.Value as SkillValidationEntity;
        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.That(entity!.ValidatedAt, Is.GreaterThan(before).And.LessThan(after));
    }

    [Test]
    public async Task CreateValidation_PersistsToDatabase()
    {
        var request = new CreateValidationRequest("TypeScript", "learner-3", "intermediate");

        await _sut.CreateValidation(request);

        var saved = Db.SkillValidations.FirstOrDefault(v => v.SkillName == "TypeScript");
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.LearnerId, Is.EqualTo("learner-3"));
        Assert.That(saved.Level, Is.EqualTo("intermediate"));
    }

    [Test]
    public async Task CreateValidation_ValidatedByIsImmutable()
    {
        // FR36: ValidatedBy is taken from ISkillForgeUser.Id — cannot be supplied or overridden via request
        // The CreateValidationRequest intentionally has no ValidatedBy field.
        _currentUser.UserId.Returns("coach-123");
        var request = new CreateValidationRequest("Go", "learner-4", "beginner");

        var result = await _sut.CreateValidation(request);

        var createdResult = result as CreatedResult;
        var entity = createdResult!.Value as SkillValidationEntity;

        // The caller cannot supply their own ValidatedBy — it always comes from the JWT identity
        Assert.That(entity!.ValidatedBy, Is.EqualTo("coach-123"),
            "ValidatedBy must always be set from authenticated user identity, not from the request body");
    }
}
