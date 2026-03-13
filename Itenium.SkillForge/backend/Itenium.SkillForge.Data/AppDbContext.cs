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

    public DbSet<SkillLevelDescriptorEntity> SkillLevelDescriptors => Set<SkillLevelDescriptorEntity>();

    public DbSet<SkillPrerequisiteEntity> SkillPrerequisites => Set<SkillPrerequisiteEntity>();

    public DbSet<ConsultantProfileEntity> ConsultantProfiles => Set<ConsultantProfileEntity>();

    public DbSet<ConsultantSkillProgressEntity> ConsultantSkillProgress => Set<ConsultantSkillProgressEntity>();

    public DbSet<SeniorityThresholdEntity> SeniorityThresholds => Set<SeniorityThresholdEntity>();

    public DbSet<SkillValidationEntity> SkillValidations => Set<SkillValidationEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // SkillPrerequisiteEntity has two FKs to SkillEntity — use Restrict to avoid cascade cycles
        builder.Entity<SkillPrerequisiteEntity>()
            .HasOne(p => p.Skill)
            .WithMany(s => s.Prerequisites)
            .HasForeignKey(p => p.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SkillPrerequisiteEntity>()
            .HasOne(p => p.PrerequisiteSkill)
            .WithMany()
            .HasForeignKey(p => p.PrerequisiteSkillId)
            .OnDelete(DeleteBehavior.Restrict);

        // Each consultant can only be assigned to one profile at a time
        builder.Entity<ConsultantProfileEntity>()
            .HasIndex(p => p.UserId)
            .IsUnique();
    }
}
