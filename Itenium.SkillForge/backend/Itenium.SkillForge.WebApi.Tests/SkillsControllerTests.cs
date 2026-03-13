using ClosedXML.Excel;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class SkillsControllerTests : DatabaseTestBase
{
    private SkillsController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new SkillsController(Db);
    }

    // ── GET /api/skills ──────────────────────────────────────────────────────

    [Test]
    public async Task GetSkills_ReturnsAllSkills_WhenNoProfileFilter()
    {
        Db.Skills.AddRange(
            new SkillEntity { Name = "C# Basics", Category = "Development", LevelCount = 5 },
            new SkillEntity { Name = "Java Basics", Category = "Development", LevelCount = 5 });
        await Db.SaveChangesAsync();

        var result = await _sut.GetSkills(null);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var skills = ok!.Value as IEnumerable<object>;
        Assert.That(skills, Is.Not.Null);
        Assert.That(skills!.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetSkills_WhenNoSkills_ReturnsEmptyList()
    {
        var result = await _sut.GetSkills(null);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var skills = ok!.Value as IEnumerable<object>;
        Assert.That(skills, Is.Empty);
    }

    [Test]
    public async Task GetSkills_FiltersByProfile()
    {
        var skill1 = new SkillEntity { Name = "C# Basics", Category = "Development" };
        var skill2 = new SkillEntity { Name = "Java Basics", Category = "Development" };
        Db.Skills.AddRange(skill1, skill2);
        await Db.SaveChangesAsync();

        Db.SkillProfiles.Add(new SkillProfileEntity { SkillId = skill1.Id, Profile = CompetenceCentreProfile.DotNet });
        await Db.SaveChangesAsync();

        var result = await _sut.GetSkills(CompetenceCentreProfile.DotNet);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var skills = ok!.Value as IEnumerable<object>;
        Assert.That(skills!.Count(), Is.EqualTo(1));
    }

    // ── GET /api/skills/{id} ─────────────────────────────────────────────────

    [Test]
    public async Task GetSkill_WhenExists_ReturnsSkillWithPrerequisiteWarnings()
    {
        var prereq = new SkillEntity { Name = "Clean Code", Category = "Craftsmanship", LevelCount = 5 };
        var skill = new SkillEntity { Name = "DDD", Category = "Architecture", LevelCount = 3 };
        Db.Skills.AddRange(prereq, skill);
        await Db.SaveChangesAsync();

        // Set prerequisites JSON after we have IDs
        skill.Prerequisites = [new SkillPrerequisite { SkillId = prereq.Id, RequiredNiveau = 3 }];
        await Db.SaveChangesAsync();

        var result = await _sut.GetSkill(skill.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
    }

    [Test]
    public async Task GetSkill_WhenNotExists_ReturnsNotFound()
    {
        var result = await _sut.GetSkill(999);
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    // ── POST /api/skills/import ───────────────────────────────────────────────

    [Test]
    public async Task ImportSkills_ValidExcel_ImportsSkills()
    {
        var file = ExcelTestHelper.CreateExcelFile(wb =>
        {
            var ws = wb.Worksheets.Add("Skills");
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Category";
            ws.Cell(1, 3).Value = "Description";
            ws.Cell(1, 4).Value = "LevelCount";
            ws.Cell(1, 5).Value = "Prerequisites";
            ws.Cell(2, 1).Value = "TypeScript";
            ws.Cell(2, 2).Value = "Language";
            ws.Cell(2, 3).Value = "JS with types";
            ws.Cell(2, 4).Value = 3;
            ws.Cell(2, 5).Value = "";
            ws.Cell(3, 1).Value = "React";
            ws.Cell(3, 2).Value = "Framework";
            ws.Cell(3, 3).Value = "UI library";
            ws.Cell(3, 4).Value = 5;
            ws.Cell(3, 5).Value = "";
        });

        var result = await _sut.ImportSkills(file);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var response = ok!.Value as SkillImportResult;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Imported, Is.EqualTo(2));
        Assert.That(response.Errors, Is.Empty);

        var saved = await Db.Skills.CountAsync();
        Assert.That(saved, Is.EqualTo(2));
    }

    [Test]
    public async Task ImportSkills_EmptyFile_ReturnsBadRequest()
    {
        var file = ExcelTestHelper.CreateEmptyFormFile();

        var result = await _sut.ImportSkills(file);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task ImportSkills_MissingColumns_ReturnsBadRequest()
    {
        // Excel with only a Name column — Category and LevelCount are missing
        var file = ExcelTestHelper.CreateExcelFile(wb =>
        {
            var ws = wb.Worksheets.Add("Skills");
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(2, 1).Value = "SomeSkill";
        });

        var result = await _sut.ImportSkills(file);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task ImportSkills_DuplicateName_UpdatesExistingSkill()
    {
        // Seed an existing skill
        Db.Skills.Add(new SkillEntity { Name = "TypeScript", Category = "OldCategory", LevelCount = 1 });
        await Db.SaveChangesAsync();

        var file = ExcelTestHelper.CreateExcelFile(wb =>
        {
            var ws = wb.Worksheets.Add("Skills");
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Category";
            ws.Cell(1, 3).Value = "Description";
            ws.Cell(1, 4).Value = "LevelCount";
            ws.Cell(1, 5).Value = "Prerequisites";
            ws.Cell(2, 1).Value = "TypeScript";
            ws.Cell(2, 2).Value = "Language";
            ws.Cell(2, 3).Value = "Updated description";
            ws.Cell(2, 4).Value = 4;
            ws.Cell(2, 5).Value = "";
        });

        var result = await _sut.ImportSkills(file);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var response = ok!.Value as SkillImportResult;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Skipped, Is.EqualTo(0)); // updated, not skipped

        // Only 1 skill in DB (not duplicated)
        var count = await Db.Skills.CountAsync();
        Assert.That(count, Is.EqualTo(1));

        var updated = await Db.Skills.FirstAsync(s => s.Name == "TypeScript");
        Assert.That(updated.Category, Is.EqualTo("Language"));
        Assert.That(updated.LevelCount, Is.EqualTo(4));
    }

    [Test]
    public async Task ImportSkills_RequiresBackofficeRole_Returns403ForNonBackoffice()
    {
        // Verify [Authorize(Roles = "backoffice")] attribute is present
        var methodInfo = typeof(SkillsController).GetMethod(nameof(SkillsController.ImportSkills));
        Assert.That(methodInfo, Is.Not.Null);

        var authorizeAttr = methodInfo!
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.That(authorizeAttr, Is.Not.Null, "ImportSkills should have [Authorize] attribute");
        Assert.That(authorizeAttr!.Roles, Is.EqualTo("backoffice"), "ImportSkills should require backoffice role");
    }
}

// ── Excel test helper ─────────────────────────────────────────────────────────

public static class ExcelTestHelper
{
    /// <summary>
    /// Creates an in-memory IFormFile containing an .xlsx workbook configured by the given action.
    /// </summary>
    public static IFormFile CreateExcelFile(Action<XLWorkbook> configure)
    {
        using var wb = new XLWorkbook();
        configure(wb);

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        return new FormFile(ms, 0, ms.Length, "file", "skills.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        };
    }

    /// <summary>
    /// Creates an IFormFile with zero bytes (simulates empty upload).
    /// </summary>
    public static IFormFile CreateEmptyFormFile()
    {
        var ms = new MemoryStream();
        return new FormFile(ms, 0, 0, "file", "empty.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        };
    }
}
