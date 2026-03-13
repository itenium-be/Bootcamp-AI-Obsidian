using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

/// <summary>
/// Manages certificates issued to learners upon course completion.
/// Certificates are automatically issued when a learner reaches 100% progress.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CertificateController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    /// <summary>
    /// Initializes a new instance of <see cref="CertificateController"/>.
    /// </summary>
    /// <param name="db">The application database context.</param>
    /// <param name="user">The current authenticated user abstraction.</param>
    public CertificateController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get certificates. Learner sees own; backoffice sees all.
    /// </summary>
    /// <returns>A list of certificates scoped to the current user or all certificates for BackOffice.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
    /// <param name="id">The unique identifier of the certificate.</param>
    /// <returns>The certificate with the specified ID.</returns>
    /// <remarks>Learners can only retrieve their own certificates; BackOffice can retrieve any certificate.</remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
