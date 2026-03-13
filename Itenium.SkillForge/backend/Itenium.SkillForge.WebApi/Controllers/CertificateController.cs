using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CertificateController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public CertificateController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get certificates. Learner sees own; backoffice sees all.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CertificateEntity>>> GetCertificates()
    {
        var query = _db.Certificates.AsQueryable();

        if (!_user.IsBackOffice)
        {
            query = query.Where(c => c.LearnerId == _user.UserId);
        }

        return Ok(await query.ToListAsync());
    }

    /// <summary>
    /// Get a specific certificate by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CertificateEntity>> GetCertificate(int id)
    {
        var cert = await _db.Certificates.FindAsync(id);
        if (cert == null)
        {
            return NotFound();
        }

        // Learners can only see their own certificates
        if (!_user.IsBackOffice && cert.LearnerId != _user.UserId)
        {
            return NotFound();
        }

        return Ok(cert);
    }
}
