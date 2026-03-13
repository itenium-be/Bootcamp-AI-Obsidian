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
        await SeedSkills(db);
        await SeedSeniorityThresholds(db);
        await app.SeedTestUsers();
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

    private static async Task SeedSkills(AppDbContext db)
    {
        if (!await db.Skills.AnyAsync())
        {
            var skills = new List<SkillEntity>
            {
                new SkillEntity
                {
                    Id = 1,
                    Name = "C# Programming",
                    Description = "Proficiency in C# language and .NET ecosystem",
                    Category = "Backend Development",
                    LevelCount = 5,
                    IsUniversal = false,
                    LevelDescriptors =
                    [
                        new SkillLevelDescriptorEntity { Level = 1, Description = "Knows basic syntax, variables, loops, and conditionals" },
                        new SkillLevelDescriptorEntity { Level = 2, Description = "Uses OOP principles, collections, and LINQ" },
                        new SkillLevelDescriptorEntity { Level = 3, Description = "Applies async/await, generics, and design patterns" },
                        new SkillLevelDescriptorEntity { Level = 4, Description = "Implements advanced patterns, performance tuning" },
                        new SkillLevelDescriptorEntity { Level = 5, Description = "Contributes to language/runtime internals; mentors others" },
                    ],
                },
                new SkillEntity
                {
                    Id = 2,
                    Name = "Java Programming",
                    Description = "Proficiency in Java language and JVM ecosystem",
                    Category = "Backend Development",
                    LevelCount = 5,
                    IsUniversal = false,
                    LevelDescriptors =
                    [
                        new SkillLevelDescriptorEntity { Level = 1, Description = "Knows basic syntax, variables, loops, and conditionals" },
                        new SkillLevelDescriptorEntity { Level = 2, Description = "Uses OOP, collections, and streams" },
                        new SkillLevelDescriptorEntity { Level = 3, Description = "Applies Spring Boot, dependency injection, and JPA" },
                        new SkillLevelDescriptorEntity { Level = 4, Description = "Implements reactive programming and microservices" },
                        new SkillLevelDescriptorEntity { Level = 5, Description = "JVM tuning; contributes to frameworks; mentors others" },
                    ],
                },
                new SkillEntity
                {
                    Id = 3,
                    Name = "Clean Code",
                    Description = "Writing readable, maintainable, and testable code",
                    Category = "Software Craftsmanship",
                    LevelCount = 4,
                    IsUniversal = true,
                    LevelDescriptors =
                    [
                        new SkillLevelDescriptorEntity { Level = 1, Description = "Follows naming conventions and keeps functions short" },
                        new SkillLevelDescriptorEntity { Level = 2, Description = "Applies SOLID principles and avoids code smells" },
                        new SkillLevelDescriptorEntity { Level = 3, Description = "Refactors legacy code; leads code review culture" },
                        new SkillLevelDescriptorEntity { Level = 4, Description = "Defines standards; coaches team on craftsmanship" },
                    ],
                },
                new SkillEntity
                {
                    Id = 4,
                    Name = "Unit Testing",
                    Description = "Writing automated unit and integration tests",
                    Category = "Quality Assurance",
                    LevelCount = 4,
                    IsUniversal = true,
                    LevelDescriptors =
                    [
                        new SkillLevelDescriptorEntity { Level = 1, Description = "Writes basic unit tests with assertions" },
                        new SkillLevelDescriptorEntity { Level = 2, Description = "Uses mocking frameworks and test doubles" },
                        new SkillLevelDescriptorEntity { Level = 3, Description = "Practices TDD; achieves meaningful coverage" },
                        new SkillLevelDescriptorEntity { Level = 4, Description = "Designs testability into architecture; introduces BDD" },
                    ],
                },
                new SkillEntity
                {
                    Id = 5,
                    Name = "Docker & Containers",
                    Description = "Containerising applications and using Docker Compose",
                    Category = "DevOps",
                    LevelCount = 3,
                    IsUniversal = true,
                    LevelDescriptors =
                    [
                        new SkillLevelDescriptorEntity { Level = 1, Description = "Runs and pulls images; uses docker-compose up" },
                        new SkillLevelDescriptorEntity { Level = 2, Description = "Writes Dockerfiles; understands layering and networking" },
                        new SkillLevelDescriptorEntity { Level = 3, Description = "Optimises images; orchestrates with Compose in CI/CD" },
                    ],
                },
                new SkillEntity
                {
                    Id = 6,
                    Name = "SQL & Databases",
                    Description = "Relational database design and query optimisation",
                    Category = "Data",
                    LevelCount = 4,
                    IsUniversal = true,
                    LevelDescriptors =
                    [
                        new SkillLevelDescriptorEntity { Level = 1, Description = "Writes SELECT, INSERT, UPDATE, DELETE queries" },
                        new SkillLevelDescriptorEntity { Level = 2, Description = "Designs normalised schemas; uses JOINs and indexes" },
                        new SkillLevelDescriptorEntity { Level = 3, Description = "Analyses query plans; tunes performance" },
                        new SkillLevelDescriptorEntity { Level = 4, Description = "Architects for high availability; partitioning strategies" },
                    ],
                },
                new SkillEntity
                {
                    Id = 7,
                    Name = "Communication",
                    Description = "Communicating clearly with teammates and stakeholders",
                    Category = "Soft Skills",
                    LevelCount = 3,
                    IsUniversal = true,
                    LevelDescriptors =
                    [
                        new SkillLevelDescriptorEntity { Level = 1, Description = "Participates in meetings; asks questions" },
                        new SkillLevelDescriptorEntity { Level = 2, Description = "Presents ideas clearly; adapts to audience" },
                        new SkillLevelDescriptorEntity { Level = 3, Description = "Facilitates workshops; influences without authority" },
                    ],
                },
                new SkillEntity
                {
                    Id = 8,
                    Name = "ASP.NET Core",
                    Description = "Building REST APIs and web applications with ASP.NET Core",
                    Category = "Backend Development",
                    LevelCount = 5,
                    IsUniversal = false,
                    Prerequisites =
                    [
                        new SkillPrerequisiteEntity { PrerequisiteSkillId = 1, RequiredLevel = 2 },
                    ],
                    LevelDescriptors =
                    [
                        new SkillLevelDescriptorEntity { Level = 1, Description = "Creates basic controllers and routes" },
                        new SkillLevelDescriptorEntity { Level = 2, Description = "Uses DI, middleware, and model validation" },
                        new SkillLevelDescriptorEntity { Level = 3, Description = "Implements auth, filters, and custom middleware" },
                        new SkillLevelDescriptorEntity { Level = 4, Description = "Designs minimal APIs; applies performance patterns" },
                        new SkillLevelDescriptorEntity { Level = 5, Description = "Contributes to framework; architects large systems" },
                    ],
                },
            };

            db.Skills.AddRange(skills);
            await db.SaveChangesAsync();

            // Reset the sequence so new inserts don't conflict with the seeded explicit IDs
            await db.Database.ExecuteSqlRawAsync(
                "SELECT setval(pg_get_serial_sequence('\"Skills\"', 'Id'), (SELECT MAX(\"Id\") FROM \"Skills\"))");
        }
    }

    private static async Task SeedSeniorityThresholds(AppDbContext db)
    {
        if (!await db.SeniorityThresholds.AnyAsync())
        {
            // .NET team (teamId = 2): C# (1), Clean Code (3), Unit Testing (4), ASP.NET Core (8), SQL (6)
            db.SeniorityThresholds.AddRange(
                // Junior .NET: 3 skills at level 1+
                new SeniorityThresholdEntity { TeamId = 2, SeniorityLevel = SeniorityLevel.Junior, SkillId = 1, MinimumLevel = 1 },
                new SeniorityThresholdEntity { TeamId = 2, SeniorityLevel = SeniorityLevel.Junior, SkillId = 3, MinimumLevel = 1 },
                new SeniorityThresholdEntity { TeamId = 2, SeniorityLevel = SeniorityLevel.Junior, SkillId = 4, MinimumLevel = 1 },

                // Medior .NET: 5 skills at level 2+
                new SeniorityThresholdEntity { TeamId = 2, SeniorityLevel = SeniorityLevel.Medior, SkillId = 1, MinimumLevel = 2 },
                new SeniorityThresholdEntity { TeamId = 2, SeniorityLevel = SeniorityLevel.Medior, SkillId = 3, MinimumLevel = 2 },
                new SeniorityThresholdEntity { TeamId = 2, SeniorityLevel = SeniorityLevel.Medior, SkillId = 4, MinimumLevel = 2 },
                new SeniorityThresholdEntity { TeamId = 2, SeniorityLevel = SeniorityLevel.Medior, SkillId = 6, MinimumLevel = 2 },
                new SeniorityThresholdEntity { TeamId = 2, SeniorityLevel = SeniorityLevel.Medior, SkillId = 8, MinimumLevel = 2 },

                // Senior .NET: 5 skills at level 3+
                new SeniorityThresholdEntity { TeamId = 2, SeniorityLevel = SeniorityLevel.Senior, SkillId = 1, MinimumLevel = 3 },
                new SeniorityThresholdEntity { TeamId = 2, SeniorityLevel = SeniorityLevel.Senior, SkillId = 3, MinimumLevel = 3 },
                new SeniorityThresholdEntity { TeamId = 2, SeniorityLevel = SeniorityLevel.Senior, SkillId = 4, MinimumLevel = 3 },
                new SeniorityThresholdEntity { TeamId = 2, SeniorityLevel = SeniorityLevel.Senior, SkillId = 6, MinimumLevel = 3 },
                new SeniorityThresholdEntity { TeamId = 2, SeniorityLevel = SeniorityLevel.Senior, SkillId = 8, MinimumLevel = 3 },

                // Java team (teamId = 1): Java (2), Clean Code (3), Unit Testing (4), Docker (5), SQL (6)
                // Junior Java: 3 skills at level 1+
                new SeniorityThresholdEntity { TeamId = 1, SeniorityLevel = SeniorityLevel.Junior, SkillId = 2, MinimumLevel = 1 },
                new SeniorityThresholdEntity { TeamId = 1, SeniorityLevel = SeniorityLevel.Junior, SkillId = 3, MinimumLevel = 1 },
                new SeniorityThresholdEntity { TeamId = 1, SeniorityLevel = SeniorityLevel.Junior, SkillId = 4, MinimumLevel = 1 },

                // Medior Java: 5 skills at level 2+
                new SeniorityThresholdEntity { TeamId = 1, SeniorityLevel = SeniorityLevel.Medior, SkillId = 2, MinimumLevel = 2 },
                new SeniorityThresholdEntity { TeamId = 1, SeniorityLevel = SeniorityLevel.Medior, SkillId = 3, MinimumLevel = 2 },
                new SeniorityThresholdEntity { TeamId = 1, SeniorityLevel = SeniorityLevel.Medior, SkillId = 4, MinimumLevel = 2 },
                new SeniorityThresholdEntity { TeamId = 1, SeniorityLevel = SeniorityLevel.Medior, SkillId = 5, MinimumLevel = 2 },
                new SeniorityThresholdEntity { TeamId = 1, SeniorityLevel = SeniorityLevel.Medior, SkillId = 6, MinimumLevel = 2 },

                // Senior Java: 5 skills at level 3+
                new SeniorityThresholdEntity { TeamId = 1, SeniorityLevel = SeniorityLevel.Senior, SkillId = 2, MinimumLevel = 3 },
                new SeniorityThresholdEntity { TeamId = 1, SeniorityLevel = SeniorityLevel.Senior, SkillId = 3, MinimumLevel = 3 },
                new SeniorityThresholdEntity { TeamId = 1, SeniorityLevel = SeniorityLevel.Senior, SkillId = 4, MinimumLevel = 3 },
                new SeniorityThresholdEntity { TeamId = 1, SeniorityLevel = SeniorityLevel.Senior, SkillId = 5, MinimumLevel = 3 },
                new SeniorityThresholdEntity { TeamId = 1, SeniorityLevel = SeniorityLevel.Senior, SkillId = 6, MinimumLevel = 3 });

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
    }
}
