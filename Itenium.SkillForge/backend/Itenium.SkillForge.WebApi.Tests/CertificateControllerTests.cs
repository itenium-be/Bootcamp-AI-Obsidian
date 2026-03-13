using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class CertificateControllerTests : DatabaseTestBase
{
    private CertificateController _sut = null!;
    private ISkillForgeUser _user = null!;

    private const string LearnerId = "learner-user-id";
    private const string OtherLearnerId = "other-learner-id";

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _user.UserId.Returns(LearnerId);
        _user.IsBackOffice.Returns(false);
        _sut = new CertificateController(Db, _user);
    }

    private async Task<CertificateEntity> CreateCert(string learnerId, string certNumber, string courseName = "Test Course")
    {
        var course = new CourseEntity { Name = courseName };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();

        var cert = new CertificateEntity
        {
            LearnerId = learnerId,
            LearnerName = "Test Name",
            CourseId = course.Id,
            CourseName = courseName,
            IssuedAt = DateTime.UtcNow,
            CertificateNumber = certNumber
        };
        Db.Certificates.Add(cert);
        await Db.SaveChangesAsync();
        return cert;
    }

    [Test]
    public async Task GetCertificates_AsLearner_ReturnsOnlyMyCertificates()
    {
        await CreateCert(LearnerId, "CERT-2026-000001");
        await CreateCert(OtherLearnerId, "CERT-2026-000002");

        var result = await _sut.GetCertificates();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var list = ok!.Value as List<CertificateEntity>;
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list![0].LearnerId, Is.EqualTo(LearnerId));
    }

    [Test]
    public async Task GetCertificates_AsBackOffice_ReturnsAll()
    {
        _user.IsBackOffice.Returns(true);
        await CreateCert(LearnerId, "CERT-2026-000001");
        await CreateCert(OtherLearnerId, "CERT-2026-000002");

        var result = await _sut.GetCertificates();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var list = ok!.Value as List<CertificateEntity>;
        Assert.That(list, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetCertificate_WhenExists_ReturnsCertificate()
    {
        var cert = await CreateCert(LearnerId, "CERT-2026-000003", "Advanced C#");

        var result = await _sut.GetCertificate(cert.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var returned = ok!.Value as CertificateEntity;
        Assert.That(returned!.CertificateNumber, Is.EqualTo("CERT-2026-000003"));
        Assert.That(returned.CourseName, Is.EqualTo("Advanced C#"));
    }

    [Test]
    public async Task GetCertificate_WhenNotExists_ReturnsNotFound()
    {
        var result = await _sut.GetCertificate(9999);
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetCertificate_OtherLearnersCert_AsLearner_ReturnsNotFound()
    {
        var cert = await CreateCert(OtherLearnerId, "CERT-2026-000004");

        var result = await _sut.GetCertificate(cert.Id);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetCertificate_OtherLearnersCert_AsBackOffice_ReturnsCert()
    {
        _user.IsBackOffice.Returns(true);
        var cert = await CreateCert(OtherLearnerId, "CERT-2026-000005");

        var result = await _sut.GetCertificate(cert.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
    }
}
