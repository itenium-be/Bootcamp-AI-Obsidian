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

    public DbSet<EnrollmentEntity> Enrollments => Set<EnrollmentEntity>();

    public DbSet<ProgressEntity> Progresses => Set<ProgressEntity>();

    public DbSet<CertificateEntity> Certificates => Set<CertificateEntity>();

    public DbSet<CourseTeamEntity> CourseTeams => Set<CourseTeamEntity>();

    public DbSet<CourseFeedbackEntity> CourseFeedbacks => Set<CourseFeedbackEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Enrollment: unique per learner+course
        builder.Entity<EnrollmentEntity>(e =>
        {
            e.HasIndex(x => new { x.LearnerId, x.CourseId }).IsUnique();
            e.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Progress: unique per learner+course
        builder.Entity<ProgressEntity>(e =>
        {
            e.HasIndex(x => new { x.LearnerId, x.CourseId }).IsUnique();
            e.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Certificate: unique certificate number
        builder.Entity<CertificateEntity>(e =>
        {
            e.HasIndex(x => x.CertificateNumber).IsUnique();
            e.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CourseTeam: unique per course+team
        builder.Entity<CourseTeamEntity>(e =>
        {
            e.HasIndex(x => new { x.CourseId, x.TeamId }).IsUnique();
            e.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Team)
                .WithMany()
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CourseFeedback: one feedback per learner per course
        builder.Entity<CourseFeedbackEntity>(e =>
        {
            e.HasIndex(x => new { x.LearnerId, x.CourseId }).IsUnique();
            e.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
