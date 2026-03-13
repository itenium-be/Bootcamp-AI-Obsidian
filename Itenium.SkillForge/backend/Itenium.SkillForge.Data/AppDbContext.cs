using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

public class AppDbContext : ForgeIdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<TeamEntity> Teams => Set<TeamEntity>();

    public DbSet<CourseEntity> Courses => Set<CourseEntity>();

    public DbSet<SkillEntity> Skills => Set<SkillEntity>();

    public DbSet<SkillProfileEntity> SkillProfiles => Set<SkillProfileEntity>();

    public DbSet<ConsultantProfileEntity> ConsultantProfiles => Set<ConsultantProfileEntity>();

    public DbSet<SeniorityThresholdEntity> SeniorityThresholds => Set<SeniorityThresholdEntity>();

    public DbSet<GoalEntity> Goals => Set<GoalEntity>();

    public DbSet<ReadinessFlagEntity> ReadinessFlags => Set<ReadinessFlagEntity>();

    // Resource Library (Stories #25-#28)
    public DbSet<ResourceEntity> Resources => Set<ResourceEntity>();

    public DbSet<ResourceCompletionEntity> ResourceCompletions => Set<ResourceCompletionEntity>();

    public DbSet<ResourceRatingEntity> ResourceRatings => Set<ResourceRatingEntity>();

    // Live Session (Stories #31-#33)
    public DbSet<CoachingSessionEntity> CoachingSessions => Set<CoachingSessionEntity>();

    public DbSet<ValidationEntity> Validations => Set<ValidationEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SkillProfileEntity>()
            .HasIndex(sp => new { sp.SkillId, sp.Profile })
            .IsUnique();

        builder.Entity<ConsultantProfileEntity>()
            .HasOne<ForgeUser>()
            .WithMany()
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ConsultantProfileEntity>()
            .HasIndex(cp => cp.UserId)
            .IsUnique();

        builder.Entity<SeniorityThresholdEntity>()
            .HasIndex(st => new { st.Profile, st.SeniorityLevel, st.SkillId })
            .IsUnique();

        builder.Entity<GoalEntity>()
            .HasOne(g => g.Skill)
            .WithMany()
            .HasForeignKey(g => g.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<GoalEntity>()
            .HasIndex(g => g.ConsultantId);

        builder.Entity<ReadinessFlagEntity>()
            .HasOne(rf => rf.Goal)
            .WithMany(g => g.ReadinessFlags)
            .HasForeignKey(rf => rf.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ReadinessFlagEntity>()
            .HasIndex(rf => new { rf.GoalId, rf.IsActive });

        // Resource Library
        builder.Entity<ResourceEntity>()
            .HasOne(r => r.Skill)
            .WithMany()
            .HasForeignKey(r => r.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ResourceCompletionEntity>()
            .HasOne(rc => rc.Resource)
            .WithMany(r => r.Completions)
            .HasForeignKey(rc => rc.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ResourceCompletionEntity>()
            .HasOne(rc => rc.Goal)
            .WithMany()
            .HasForeignKey(rc => rc.GoalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ResourceRatingEntity>()
            .HasOne(rr => rr.Resource)
            .WithMany(r => r.Ratings)
            .HasForeignKey(rr => rr.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // One rating per user per resource
        builder.Entity<ResourceRatingEntity>()
            .HasIndex(rr => new { rr.ResourceId, rr.UserId })
            .IsUnique();

        // Live Session
        builder.Entity<ValidationEntity>()
            .HasOne(v => v.Skill)
            .WithMany()
            .HasForeignKey(v => v.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ValidationEntity>()
            .HasOne(v => v.Session)
            .WithMany(s => s.Validations)
            .HasForeignKey(v => v.SessionId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.Entity<ValidationEntity>()
            .HasIndex(v => v.ConsultantId);

        builder.Entity<CoachingSessionEntity>()
            .HasIndex(cs => cs.ConsultantId);

        // Story #36/#37: Archive fields on the user table (shadow properties on ForgeUser).
        // IsArchived disables login; ArchivedAt records when the account was archived.
        // All coaching history is preserved — no hard deletion.
        builder.Entity<ForgeUser>()
            .Property<bool>("IsArchived")
            .HasDefaultValue(false);

        builder.Entity<ForgeUser>()
            .Property<DateTime?>("ArchivedAt");

        builder.Entity<ForgeUser>()
            .HasIndex("IsArchived");
    }
}
