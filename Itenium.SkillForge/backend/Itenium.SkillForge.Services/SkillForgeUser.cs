using System.Globalization;
using Itenium.Forge.Security;
using Microsoft.AspNetCore.Http;

namespace Itenium.SkillForge.Services;

/// <summary>
/// Claims-based implementation of <see cref="ISkillForgeUser"/>.
/// Extracts user identity and permissions from the current HTTP context's JWT claims.
/// </summary>
public class SkillForgeUser : CurrentUser, ISkillForgeUser
{
    /// <summary>
    /// Initializes a new instance of <see cref="SkillForgeUser"/>.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor used to access the current request principal.</param>
    public SkillForgeUser(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
    }

    /// <inheritdoc/>
    public bool IsBackOffice => User?.IsInRole("backoffice") ?? false;

    /// <inheritdoc/>
    public ICollection<int> Teams
    {
        get
        {
            if (User == null)
            {
                return [];
            }

            var teams = User.FindAll("team").Select(c => int.Parse(c.Value, CultureInfo.InvariantCulture)).ToArray();
            return teams;
        }
    }
}
