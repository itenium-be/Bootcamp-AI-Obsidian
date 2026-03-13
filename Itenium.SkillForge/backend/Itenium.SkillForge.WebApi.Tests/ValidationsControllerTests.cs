using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

/// <summary>
/// Story #14: Restrict skill validation writes to Coach (manager) role only.
/// FR4: POST /api/validations returns 403 for learner and backoffice.
/// FR36: ValidatedBy and ValidatedAt are set server-side and immutable.
/// </summary>
[TestFixture]
public class ValidationsControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private ValidationsController _sut = null!;
    private SkillEntity _skill = null!;

    [SetUp]
    public async Task Setup()
    {
        _skill = new SkillEntity { Name = "C#", Category = "Development", LevelCount = 3 };
        Db.Skills.Add(_skill);
        await Db.SaveChangesAsync();

        _user = Substitute.For<ISkillForgeUser>();
        _sut = new ValidationsController(Db, _user);
    }

    // --- POST /api/validations ---

    [Test]
    public async Task CreateValidation_WhenManager_ReturnsCreated()
    {
        _user.IsManager.Returns(true);
        _user.UserId.Returns("coach-user-id");

        var request = new CreateValidationRequest(
            SkillId: _skill.Id,
            ConsultantId: "consultant-1",
            FromNiveau: 1,
            ToNiveau: 2,
            Notes: "Good progress",
            SessionId: null);

        var result = await _sut.CreateValidation(request);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var validation = created!.Value as ValidationEntity;
        Assert.That(validation!.ConsultantId, Is.EqualTo("consultant-1"));
        Assert.That(validation.SkillId, Is.EqualTo(_skill.Id));
    }

    [Test]
    public async Task CreateValidation_WhenManager_SetsValidatedByFromCurrentUser_NotRequest()
    {
        // FR36: ValidatedBy is always the authenticated coach — cannot be spoofed from request body.
        _user.IsManager.Returns(true);
        _user.UserId.Returns("real-coach-id");

        var request = new CreateValidationRequest(
            SkillId: _skill.Id,
            ConsultantId: "consultant-1",
            FromNiveau: 1,
            ToNiveau: 2,
            Notes: null,
            SessionId: null);

        var result = await _sut.CreateValidation(request);

        var created = result.Result as CreatedAtActionResult;
        var validation = created!.Value as ValidationEntity;
        Assert.That(validation!.ValidatedBy, Is.EqualTo("real-coach-id"));
    }

    [Test]
    public async Task CreateValidation_WhenManager_SetsValidatedAtServerSide()
    {
        // FR36: ValidatedAt is always server-side UTC — cannot be spoofed.
        var before = DateTime.UtcNow.AddSeconds(-1);
        _user.IsManager.Returns(true);
        _user.UserId.Returns("coach-id");

        var request = new CreateValidationRequest(
            SkillId: _skill.Id,
            ConsultantId: "consultant-1",
            FromNiveau: 1,
            ToNiveau: 2,
            Notes: null,
            SessionId: null);

        var result = await _sut.CreateValidation(request);

        var created = result.Result as CreatedAtActionResult;
        var validation = created!.Value as ValidationEntity;
        Assert.That(validation!.ValidatedAt, Is.GreaterThan(before));
    }

    [Test]
    public async Task CreateValidation_WhenLearner_ReturnsForbidden()
    {
        // FR4: learner role cannot write validations.
        _user.IsManager.Returns(false);

        var request = new CreateValidationRequest(
            SkillId: _skill.Id,
            ConsultantId: "consultant-1",
            FromNiveau: 1,
            ToNiveau: 2,
            Notes: null,
            SessionId: null);

        var result = await _sut.CreateValidation(request);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task CreateValidation_WhenBackOffice_ReturnsForbidden()
    {
        // FR4: backoffice role cannot write validations.
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(true);

        var request = new CreateValidationRequest(
            SkillId: _skill.Id,
            ConsultantId: "consultant-1",
            FromNiveau: 1,
            ToNiveau: 2,
            Notes: null,
            SessionId: null);

        var result = await _sut.CreateValidation(request);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task CreateValidation_PersistsToDatabase()
    {
        _user.IsManager.Returns(true);
        _user.UserId.Returns("coach-id");

        var request = new CreateValidationRequest(
            SkillId: _skill.Id,
            ConsultantId: "consultant-persist",
            FromNiveau: 2,
            ToNiveau: 3,
            Notes: "Validated during session",
            SessionId: null);

        await _sut.CreateValidation(request);

        var saved = Db.Validations.FirstOrDefault(v => v.ConsultantId == "consultant-persist");
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.FromNiveau, Is.EqualTo(2));
        Assert.That(saved.ToNiveau, Is.EqualTo(3));
        Assert.That(saved.Notes, Is.EqualTo("Validated during session"));
    }

    // --- GET /api/validations ---

    [Test]
    public async Task GetValidations_ByConsultantId_ReturnsMatchingValidations()
    {
        Db.Validations.AddRange(
            new ValidationEntity { SkillId = _skill.Id, ConsultantId = "c1", ValidatedBy = "coach", ValidatedAt = DateTime.UtcNow, FromNiveau = 1, ToNiveau = 2 },
            new ValidationEntity { SkillId = _skill.Id, ConsultantId = "c1", ValidatedBy = "coach", ValidatedAt = DateTime.UtcNow, FromNiveau = 2, ToNiveau = 3 },
            new ValidationEntity { SkillId = _skill.Id, ConsultantId = "c2", ValidatedBy = "coach", ValidatedAt = DateTime.UtcNow, FromNiveau = 1, ToNiveau = 3 });
        await Db.SaveChangesAsync();

        var result = await _sut.GetValidations("c1");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var validations = ok!.Value as List<ValidationEntity>;
        Assert.That(validations, Has.Count.EqualTo(2));
        Assert.That(validations!.All(v => v.ConsultantId == "c1"), Is.True);
    }

    [Test]
    public async Task GetValidations_WhenNoFilter_ReturnsAll()
    {
        Db.Validations.AddRange(
            new ValidationEntity { SkillId = _skill.Id, ConsultantId = "c1", ValidatedBy = "coach", ValidatedAt = DateTime.UtcNow, FromNiveau = 1, ToNiveau = 2 },
            new ValidationEntity { SkillId = _skill.Id, ConsultantId = "c2", ValidatedBy = "coach", ValidatedAt = DateTime.UtcNow, FromNiveau = 1, ToNiveau = 2 });
        await Db.SaveChangesAsync();

        var result = await _sut.GetValidations(null);

        var ok = result.Result as OkObjectResult;
        var validations = ok!.Value as List<ValidationEntity>;
        Assert.That(validations, Has.Count.EqualTo(2));
    }
}
