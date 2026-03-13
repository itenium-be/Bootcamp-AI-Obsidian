using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class SeniorityControllerTests : DatabaseTestBase
{
    private SeniorityController _sut = null!;
    private SkillEntity _skill1 = null!;
    private SkillEntity _skill2 = null!;

    [SetUp]
    public async Task Setup()
    {
        _skill1 = new SkillEntity { Name = "C# Basics", Category = "Core", LevelCount = 5 };
        _skill2 = new SkillEntity { Name = "LINQ", Category = "Core", LevelCount = 3 };
        Db.Skills.AddRange(_skill1, _skill2);
        await Db.SaveChangesAsync();

        _sut = new SeniorityController(Db);
    }

    // ── GET /api/seniority/{profile} ─────────────────────────────────────────

    [Test]
    public async Task GetThresholds_ReturnsAllThresholdsForProfile()
    {
        Db.SeniorityThresholds.AddRange(
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Junior, SkillId = _skill1.Id, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Medior, SkillId = _skill1.Id, MinNiveau = 3 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Junior, SkillId = _skill2.Id, MinNiveau = 1 });
        await Db.SaveChangesAsync();

        var result = await _sut.GetThresholds(CompetenceCentreProfile.DotNet);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var ruleset = ok!.Value as SeniorityRulesetResponse;
        Assert.That(ruleset, Is.Not.Null);
        Assert.That(ruleset!.Profile, Is.EqualTo(CompetenceCentreProfile.DotNet));
        Assert.That(ruleset.Thresholds, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetThresholds_WhenNoThresholds_ReturnsEmptyList()
    {
        var result = await _sut.GetThresholds(CompetenceCentreProfile.QA);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var ruleset = ok!.Value as SeniorityRulesetResponse;
        Assert.That(ruleset!.Thresholds, Is.Empty);
    }

    // ── GET /api/seniority/progress ──────────────────────────────────────────

    [Test]
    public async Task GetProgress_ComputesMediorProgress_FromGoals()
    {
        // Setup profile
        Db.ConsultantProfiles.Add(new ConsultantProfileEntity
        {
            UserId = "user-lea",
            Profile = CompetenceCentreProfile.DotNet,
        });

        // Setup Medior thresholds: 2 skills required
        Db.SeniorityThresholds.AddRange(
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Medior, SkillId = _skill1.Id, MinNiveau = 3 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Medior, SkillId = _skill2.Id, MinNiveau = 2 });

        // Lea meets skill1 (niveau 3) but not skill2 (niveau 1 < 2)
        Db.Goals.AddRange(
            new GoalEntity { ConsultantId = "user-lea", CoachId = "coach", SkillId = _skill1.Id, CurrentNiveau = 3, TargetNiveau = 5, Deadline = DateTime.UtcNow.AddMonths(6) },
            new GoalEntity { ConsultantId = "user-lea", CoachId = "coach", SkillId = _skill2.Id, CurrentNiveau = 1, TargetNiveau = 3, Deadline = DateTime.UtcNow.AddMonths(3) });

        await Db.SaveChangesAsync();

        var result = await _sut.GetProgress("user-lea");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var progress = ok!.Value as SeniorityProgressResponse;
        Assert.That(progress, Is.Not.Null);
        Assert.That(progress!.MetCount, Is.EqualTo(1));
        Assert.That(progress.RequiredCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetProgress_WhenNoProfile_ReturnsNoProgress()
    {
        var result = await _sut.GetProgress("user-noprofile");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var progress = ok!.Value as SeniorityProgressResponse;
        Assert.That(progress!.MetCount, Is.EqualTo(0));
        Assert.That(progress.RequiredCount, Is.EqualTo(0));
    }
}
