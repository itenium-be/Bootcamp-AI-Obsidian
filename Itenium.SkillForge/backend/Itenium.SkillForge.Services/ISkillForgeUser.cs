using Itenium.Forge.Security;

namespace Itenium.SkillForge.Services;

/// <summary>
/// Provides access to the current user.
/// </summary>
public interface ISkillForgeUser : ICurrentUser
{
    /// <summary>
    /// Gets a value indicating whether the current user is BackOffice management.
    /// BackOffice has platform-wide access (FR3).
    /// </summary>
    bool IsBackOffice { get; }

    /// <summary>
    /// Gets a value indicating whether the current user is a Coach/Manager.
    /// Managers have team-scoped access (FR2).
    /// </summary>
    bool IsManager { get; }

    /// <summary>
    /// Gets a value indicating whether the current user is a Consultant/Learner.
    /// Consultants can only access their own data (FR1).
    /// </summary>
    bool IsConsultant { get; }

    /// <summary>
    /// Gets the IDs of the Teams the user has access to.
    /// Team scoping enforced at repository layer via this claim (FR2).
    /// </summary>
    ICollection<int> Teams { get; }
}
