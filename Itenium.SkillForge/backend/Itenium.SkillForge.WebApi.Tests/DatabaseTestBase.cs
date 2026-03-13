using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Itenium.SkillForge.WebApi.Tests;

public abstract class DatabaseTestBase
{
    private IDbContextTransaction _transaction = null!;

    protected AppDbContext Db { get; private set; } = null!;

    /// <summary>
    /// Creates a minimal ForgeUser row required for ConsultantProfile FK constraints.
    /// </summary>
    protected async Task<ForgeUser> CreateTestUser(string userId)
    {
        var user = new ForgeUser { Id = userId, UserName = userId };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user;
    }

    [SetUp]
    public async Task BaseSetUp()
    {
        Db = new AppDbContext(PostgresFixture.CreateDbContextOptions());
        _transaction = await Db.Database.BeginTransactionAsync();
    }

    [TearDown]
    public async Task BaseTearDown()
    {
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await Db.DisposeAsync();
    }
}
