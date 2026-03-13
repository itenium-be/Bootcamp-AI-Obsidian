using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Itenium.SkillForge.Data;

public static class SeedData
{
    public static async Task SeedDevelopmentData(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedTeams(db);
        await SeedCourses(db);
        await SkillSeedData.SeedSkills(db);
        await app.SeedTestUsers();
        await SeedDemoGoals(app, db);
    }

    private static async Task SeedTeams(AppDbContext db)
    {
        if (!await db.Teams.AnyAsync())
        {
            db.Teams.AddRange(
                new TeamEntity { Id = 1, Name = "Java" },
                new TeamEntity { Id = 2, Name = ".NET" },
                new TeamEntity { Id = 3, Name = "PO & Analysis" },
                new TeamEntity { Id = 4, Name = "QA" });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedCourses(AppDbContext db)
    {
        if (!await db.Courses.AnyAsync())
        {
            db.Courses.AddRange(
                new CourseEntity { Id = 1, Name = "Introduction to Programming", Description = "Learn the basics of programming", Category = "Development", Level = "Beginner" },
                new CourseEntity { Id = 2, Name = "Advanced C#", Description = "Master C# programming language", Category = "Development", Level = "Advanced" },
                new CourseEntity { Id = 3, Name = "Cloud Architecture", Description = "Design scalable cloud solutions", Category = "Architecture", Level = "Intermediate" },
                new CourseEntity { Id = 4, Name = "Agile Project Management", Description = "Learn agile methodologies", Category = "Management", Level = "Beginner" });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedTestUsers(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ForgeUser>>();

        // BackOffice admin - no team claim (manages all)
        if (await userManager.FindByEmailAsync("backoffice@test.local") == null)
        {
            var admin = new ForgeUser
            {
                UserName = "backoffice",
                Email = "backoffice@test.local",
                EmailConfirmed = true,
                FirstName = "BackOffice",
                LastName = "Admin"
            };
            var result = await userManager.CreateAsync(admin, "AdminPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRolesAsync(admin, ["backoffice"]);
            }
        }

        // Local user for Java team only
        if (await userManager.FindByEmailAsync("java@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "java",
                Email = "java@test.local",
                EmailConfirmed = true,
                FirstName = "Java",
                LastName = "Developer"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "manager");
                await userManager.AddClaimAsync(user, new Claim("team", "1")); // Java
            }
        }

        // Local user for .NET team only
        if (await userManager.FindByEmailAsync("dotnet@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "dotnet",
                Email = "dotnet@test.local",
                EmailConfirmed = true,
                FirstName = "DotNet",
                LastName = "Developer"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "manager");
                await userManager.AddClaimAsync(user, new Claim("team", "2")); // .NET
            }
        }

        // User with access to multiple teams (Java + .NET)
        if (await userManager.FindByEmailAsync("multi@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "multi",
                Email = "multi@test.local",
                EmailConfirmed = true,
                FirstName = "Multi",
                LastName = "Team"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "manager");
                await userManager.AddClaimAsync(user, new Claim("team", "1")); // Java
                await userManager.AddClaimAsync(user, new Claim("team", "2")); // .NET
            }
        }

        // Learner user - basic learner role
        if (await userManager.FindByEmailAsync("learner@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "learner",
                Email = "learner@test.local",
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "Learner"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "learner");
            }
        }

        // Journey 1: Lea — .NET consultant with pre-populated goals
        if (await userManager.FindByEmailAsync("lea@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "lea",
                Email = "lea@test.local",
                EmailConfirmed = true,
                FirstName = "Lea",
                LastName = "Demo"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "learner");
            }
        }

        // Journey 4: Sander — Java consultant with pre-populated goals
        if (await userManager.FindByEmailAsync("sander@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "sander",
                Email = "sander@test.local",
                EmailConfirmed = true,
                FirstName = "Sander",
                LastName = "Demo"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "learner");
            }
        }

        // Journey 2: Nathalie — coach for Lea and Sander
        if (await userManager.FindByEmailAsync("nathalie@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "nathalie",
                Email = "nathalie@test.local",
                EmailConfirmed = true,
                FirstName = "Nathalie",
                LastName = "Coach"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "manager");
                await userManager.AddClaimAsync(user, new Claim("team", "1")); // Java
                await userManager.AddClaimAsync(user, new Claim("team", "2")); // .NET
            }
        }
    }

    /// <summary>
    /// Seed demo goals for Journey 1 (Lea) and Journey 4 (Sander). Story #21.
    /// </summary>
    private static async Task SeedDemoGoals(WebApplication app, AppDbContext db)
    {
        if (await db.Goals.AnyAsync())
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ForgeUser>>();

        var lea = await userManager.FindByEmailAsync("lea@test.local");
        var sander = await userManager.FindByEmailAsync("sander@test.local");
        var nathalie = await userManager.FindByEmailAsync("nathalie@test.local");

        if (lea == null || sander == null || nathalie == null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        // Journey 1: Lea — 3 .NET goals already set by coach
        // SkillIds match SkillSeedData: C# Fundamentals=10, ASP.NET Core=11, .NET Testing=13
        db.Goals.AddRange(
            new GoalEntity
            {
                ConsultantId = lea.Id,
                CoachId = nathalie.Id,
                SkillId = 10, // C# Fundamentals
                CurrentNiveau = 1,
                TargetNiveau = 3,
                Deadline = now.AddMonths(2),
                CreatedAt = now.AddDays(-1),
                Status = GoalStatus.Active,
            },
            new GoalEntity
            {
                ConsultantId = lea.Id,
                CoachId = nathalie.Id,
                SkillId = 11, // ASP.NET Core Web API
                CurrentNiveau = 1,
                TargetNiveau = 2,
                Deadline = now.AddMonths(3),
                CreatedAt = now.AddDays(-1),
                Status = GoalStatus.Active,
            },
            new GoalEntity
            {
                ConsultantId = lea.Id,
                CoachId = nathalie.Id,
                SkillId = 13, // .NET Testing
                CurrentNiveau = 0,
                TargetNiveau = 2,
                Deadline = now.AddMonths(6),
                CreatedAt = now.AddDays(-1),
                Status = GoalStatus.Active,
            });

        // Journey 4: Sander — 3 Java goals for first 6 weeks
        // SkillIds match SkillSeedData: Java Fundamentals=20, Spring Boot=21, Java Testing=22
        db.Goals.AddRange(
            new GoalEntity
            {
                ConsultantId = sander.Id,
                CoachId = nathalie.Id,
                SkillId = 20, // Java Fundamentals
                CurrentNiveau = 1,
                TargetNiveau = 3,
                Deadline = now.AddDays(42),
                CreatedAt = now.AddDays(-1),
                Status = GoalStatus.Active,
            },
            new GoalEntity
            {
                ConsultantId = sander.Id,
                CoachId = nathalie.Id,
                SkillId = 21, // Spring Boot
                CurrentNiveau = 1,
                TargetNiveau = 2,
                Deadline = now.AddDays(42),
                CreatedAt = now.AddDays(-1),
                Status = GoalStatus.Active,
            },
            new GoalEntity
            {
                ConsultantId = sander.Id,
                CoachId = nathalie.Id,
                SkillId = 22, // Java Testing (JUnit/Mockito)
                CurrentNiveau = 0,
                TargetNiveau = 1,
                Deadline = now.AddDays(42),
                CreatedAt = now.AddDays(-1),
                Status = GoalStatus.Active,
            });

        await db.SaveChangesAsync();

        // Journey 4: Assign Sander to Java profile (Story #19)
        // Journey 1: Assign Lea to .NET profile
        if (!await db.ConsultantProfiles.AnyAsync())
        {
            if (lea != null)
            {
                db.ConsultantProfiles.Add(new ConsultantProfileEntity
                {
                    UserId = lea.Id,
                    Profile = CompetenceCentreProfile.DotNet,
                    AssignedBy = nathalie?.Id,
                });
            }

            if (sander != null)
            {
                db.ConsultantProfiles.Add(new ConsultantProfileEntity
                {
                    UserId = sander.Id,
                    Profile = CompetenceCentreProfile.Java,
                    AssignedBy = nathalie?.Id,
                });
            }

            await db.SaveChangesAsync();
        }
    }
}
