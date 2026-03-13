using Itenium.SkillForge.Entities;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

/// <summary>
/// FR9: Idempotent seed of the global skill catalogue.
/// Covers all 4 competence centre profiles with realistic skills, level descriptors,
/// prerequisites, and seniority thresholds.
/// Story #18: seed is idempotent — re-running produces no duplicates.
/// </summary>
public static class SkillSeedData
{
    public static async Task SeedSkills(AppDbContext db)
    {
        if (await db.Skills.AnyAsync())
        {
            return;
        }

        // ── Universal / Cross-profile skills (IDs 1–9) ─────────────────────

        var cleanCode = new SkillEntity
        {
            Id = 1,
            Name = "Clean Code",
            Category = "Craftsmanship",
            Description = "Writing readable, maintainable code: SOLID, DRY, KISS.",
            LevelCount = 5,
            LevelDescriptors = [
                "Aware of clean code principles",
                "Applies naming conventions and formatting",
                "Applies SOLID principles consistently",
                "Mentors others on clean code practices",
                "Drives clean code culture across the organisation",
            ],
        };

        var git = new SkillEntity
        {
            Id = 2,
            Name = "Git & Version Control",
            Category = "Tooling",
            Description = "Using Git for source control, branching strategies, and collaboration.",
            LevelCount = 4,
            LevelDescriptors = [
                "Basic commits, push/pull",
                "Branching, merging, resolving conflicts",
                "Git Flow / trunk-based, rebase, bisect",
                "Designs branching strategies for teams",
            ],
        };

        var agile = new SkillEntity
        {
            Id = 3,
            Name = "Agile / Scrum",
            Category = "Process",
            Description = "Applying agile methodology and Scrum ceremonies effectively.",
            LevelCount = 4,
            LevelDescriptors = [
                "Participates in ceremonies",
                "Understands sprint goals and velocity",
                "Facilitates retrospectives and refinements",
                "Coaches teams on agile practices",
            ],
        };

        // ── .NET profile skills (IDs 10–19) ─────────────────────────────────

        var csharp = new SkillEntity
        {
            Id = 10,
            Name = "C# Fundamentals",
            Category = "Language",
            Description = "Core C# features: types, LINQ, async/await, generics.",
            LevelCount = 5,
            LevelDescriptors = [
                "Writes basic C# programs",
                "Uses collections, LINQ, exception handling",
                "Applies generics, delegates, events",
                "Uses advanced features: Span<T>, pattern matching, records",
                "Defines C# best-practice standards for the team",
            ],
        };

        var aspNetCore = new SkillEntity
        {
            Id = 11,
            Name = "ASP.NET Core Web API",
            Category = "Framework",
            Description = "Building RESTful APIs with ASP.NET Core, middleware, DI, auth.",
            LevelCount = 5,
            LevelDescriptors = [
                "Scaffolds basic controllers and routes",
                "Uses DI, middleware, model validation",
                "Implements JWT auth, filters, versioning",
                "Designs multi-tenant or micro-service APIs",
                "Defines API standards and governance",
            ],
            Prerequisites = [new SkillPrerequisite { SkillId = 10, RequiredNiveau = 2 }],
        };

        var efCore = new SkillEntity
        {
            Id = 12,
            Name = "Entity Framework Core",
            Category = "Data Access",
            Description = "ORM with EF Core: migrations, relationships, performance tuning.",
            LevelCount = 4,
            LevelDescriptors = [
                "Basic CRUD with DbContext",
                "Configures relationships and migrations",
                "Optimises queries, raw SQL where needed",
                "Advanced patterns: table splitting, owned entities",
            ],
            Prerequisites = [new SkillPrerequisite { SkillId = 10, RequiredNiveau = 2 }],
        };

        var dotnetTesting = new SkillEntity
        {
            Id = 13,
            Name = ".NET Testing (NUnit/xUnit)",
            Category = "Quality",
            Description = "Unit and integration testing: mocking, Testcontainers.",
            LevelCount = 4,
            LevelDescriptors = [
                "Writes basic unit tests",
                "Uses mocking frameworks (NSubstitute/Moq)",
                "Writes integration tests with Testcontainers",
                "Defines testing strategies for .NET teams",
            ],
            Prerequisites = [new SkillPrerequisite { SkillId = 10, RequiredNiveau = 1 }],
        };

        var docker = new SkillEntity
        {
            Id = 14,
            Name = "Docker & Containers",
            Category = "DevOps",
            Description = "Containerising .NET apps, Docker Compose, multi-stage builds.",
            LevelCount = 3,
            LevelDescriptors = [
                "Runs and builds Docker images",
                "Writes Dockerfiles and Compose for local dev",
                "Optimises images, CI/CD pipeline integration",
            ],
        };

        var designPatterns = new SkillEntity
        {
            Id = 15,
            Name = "Design Patterns (.NET)",
            Category = "Architecture",
            Description = "GoF patterns, CQRS, MediatR, Decorator, Strategy in .NET context.",
            LevelCount = 4,
            LevelDescriptors = [
                "Recognises and applies basic GoF patterns",
                "Applies CQRS and Mediator patterns",
                "Evaluates trade-offs between patterns",
                "Defines architectural patterns for the team",
            ],
            Prerequisites = [
                new SkillPrerequisite { SkillId = 10, RequiredNiveau = 3 },
                new SkillPrerequisite { SkillId = 1, RequiredNiveau = 2 },
            ],
        };

        // ── Java profile skills (IDs 20–29) ──────────────────────────────────

        var javaFundamentals = new SkillEntity
        {
            Id = 20,
            Name = "Java Fundamentals",
            Category = "Language",
            Description = "Core Java: OOP, collections, streams, generics, lambdas.",
            LevelCount = 5,
            LevelDescriptors = [
                "Writes basic Java programs",
                "Uses collections and streams",
                "Applies generics, lambdas, functional interfaces",
                "Uses advanced features: reflection, annotations, records",
                "Defines Java best-practice standards for the team",
            ],
        };

        var springBoot = new SkillEntity
        {
            Id = 21,
            Name = "Spring Boot",
            Category = "Framework",
            Description = "REST APIs and services with Spring Boot, IoC, JPA.",
            LevelCount = 5,
            LevelDescriptors = [
                "Scaffolds basic Spring Boot applications",
                "Uses DI, REST controllers, JPA repositories",
                "Configures security, caching, async processing",
                "Designs multi-module Spring applications",
                "Defines Spring architecture standards",
            ],
            Prerequisites = [new SkillPrerequisite { SkillId = 20, RequiredNiveau = 2 }],
        };

        var javaTesting = new SkillEntity
        {
            Id = 22,
            Name = "Java Testing (JUnit/Mockito)",
            Category = "Quality",
            Description = "Unit and integration testing in Java: JUnit 5, Mockito, Testcontainers.",
            LevelCount = 4,
            LevelDescriptors = [
                "Writes basic unit tests",
                "Uses Mockito for mocking",
                "Writes integration tests with Testcontainers",
                "Defines testing standards for Java teams",
            ],
            Prerequisites = [new SkillPrerequisite { SkillId = 20, RequiredNiveau = 1 }],
        };

        var maven = new SkillEntity
        {
            Id = 23,
            Name = "Maven / Gradle",
            Category = "Tooling",
            Description = "Build tooling: dependency management, multi-module builds, plugins.",
            LevelCount = 3,
            LevelDescriptors = [
                "Understands POM/build.gradle basics",
                "Manages dependencies and multi-module builds",
                "Creates custom plugins and optimises pipelines",
            ],
        };

        var microservicesJava = new SkillEntity
        {
            Id = 24,
            Name = "Microservices (Java/Spring Cloud)",
            Category = "Architecture",
            Description = "Microservices with Spring Cloud: discovery, gateway, circuit breakers.",
            LevelCount = 4,
            LevelDescriptors = [
                "Understands microservices principles",
                "Implements service discovery and API gateway",
                "Applies circuit breakers and distributed tracing",
                "Designs resilient microservice landscapes",
            ],
            Prerequisites = [
                new SkillPrerequisite { SkillId = 21, RequiredNiveau = 3 },
                new SkillPrerequisite { SkillId = 1, RequiredNiveau = 3 },
            ],
        };

        var javaKafka = new SkillEntity
        {
            Id = 25,
            Name = "Kafka / Messaging (Java)",
            Category = "Integration",
            Description = "Event-driven architecture with Kafka and Spring Kafka.",
            LevelCount = 3,
            LevelDescriptors = [
                "Produces and consumes basic messages",
                "Designs topics, partitions, and consumer groups",
                "Implements exactly-once and dead letter queues",
            ],
            Prerequisites = [new SkillPrerequisite { SkillId = 21, RequiredNiveau = 2 }],
        };

        // ── PO & Analysis profile skills (IDs 30–39) ─────────────────────────

        var requirements = new SkillEntity
        {
            Id = 30,
            Name = "Requirements Engineering",
            Category = "Analysis",
            Description = "Eliciting, documenting, and validating functional and non-functional requirements.",
            LevelCount = 4,
            LevelDescriptors = [
                "Documents requirements as user stories",
                "Conducts stakeholder interviews and workshops",
                "Writes acceptance criteria and BDD scenarios",
                "Defines requirements governance frameworks",
            ],
        };

        var domainModelling = new SkillEntity
        {
            Id = 31,
            Name = "Domain Modelling",
            Category = "Analysis",
            Description = "Event storming, context mapping, domain-driven analysis.",
            LevelCount = 4,
            LevelDescriptors = [
                "Identifies entities and relationships",
                "Facilitates event storming sessions",
                "Creates bounded context maps",
                "Applies DDD strategic patterns",
            ],
            Prerequisites = [new SkillPrerequisite { SkillId = 30, RequiredNiveau = 2 }],
        };

        var productOwnership = new SkillEntity
        {
            Id = 32,
            Name = "Product Ownership",
            Category = "Product",
            Description = "Managing a product backlog, prioritisation, stakeholder communication.",
            LevelCount = 5,
            LevelDescriptors = [
                "Maintains a prioritised backlog",
                "Writes clear sprint goals",
                "Balances technical debt and feature delivery",
                "Manages multiple stakeholders and trade-offs",
                "Drives product vision and roadmap",
            ],
        };

        var umlModelling = new SkillEntity
        {
            Id = 33,
            Name = "UML & Process Modelling",
            Category = "Analysis",
            Description = "Use case, sequence, activity, and BPMN diagrams.",
            LevelCount = 3,
            LevelDescriptors = [
                "Reads and creates basic UML diagrams",
                "Produces system-level sequence and activity diagrams",
                "Models complex business processes in BPMN",
            ],
        };

        var stakeholderMgmt = new SkillEntity
        {
            Id = 34,
            Name = "Stakeholder Management",
            Category = "Communication",
            Description = "Identifying stakeholders, managing expectations, conflict resolution.",
            LevelCount = 4,
            LevelDescriptors = [
                "Identifies and lists stakeholders",
                "Communicates status and issues proactively",
                "Manages conflicting priorities between stakeholders",
                "Coaches others on stakeholder engagement",
            ],
        };

        var valueStream = new SkillEntity
        {
            Id = 35,
            Name = "Value Stream Mapping",
            Category = "Analysis",
            Description = "Visualising and optimising the flow of value in software delivery.",
            LevelCount = 3,
            LevelDescriptors = [
                "Reads and interprets value stream maps",
                "Facilitates value stream mapping workshops",
                "Defines improvement roadmaps based on VSM",
            ],
            Prerequisites = [new SkillPrerequisite { SkillId = 30, RequiredNiveau = 2 }],
        };

        // ── QA profile skills (IDs 40–49) ────────────────────────────────────

        var manualTesting = new SkillEntity
        {
            Id = 40,
            Name = "Manual Testing",
            Category = "Testing",
            Description = "Test case design, execution, defect reporting, exploratory testing.",
            LevelCount = 4,
            LevelDescriptors = [
                "Executes test cases and reports defects",
                "Designs test cases from requirements",
                "Applies risk-based and exploratory testing",
                "Defines manual testing standards for the team",
            ],
        };

        var testAutomation = new SkillEntity
        {
            Id = 41,
            Name = "Test Automation (Playwright/Selenium)",
            Category = "Testing",
            Description = "Automated UI testing with Playwright or Selenium, CI integration.",
            LevelCount = 5,
            LevelDescriptors = [
                "Writes basic test scripts",
                "Builds maintainable page-object models",
                "Implements parallel execution and reporting",
                "Designs test automation frameworks from scratch",
                "Defines automation strategy for the organisation",
            ],
            Prerequisites = [new SkillPrerequisite { SkillId = 40, RequiredNiveau = 2 }],
        };

        var apiTesting = new SkillEntity
        {
            Id = 42,
            Name = "API Testing",
            Category = "Testing",
            Description = "REST API validation with Postman, RestAssured, contract testing.",
            LevelCount = 4,
            LevelDescriptors = [
                "Uses Postman for basic API calls",
                "Creates test collections with assertions",
                "Implements contract testing (Pact/OpenAPI)",
                "Defines API test strategy",
            ],
        };

        var performanceTesting = new SkillEntity
        {
            Id = 43,
            Name = "Performance Testing",
            Category = "Testing",
            Description = "Load, stress, and spike testing with k6 or JMeter.",
            LevelCount = 3,
            LevelDescriptors = [
                "Runs existing performance test scripts",
                "Designs load test scenarios and interprets results",
                "Defines performance budgets and CI gating",
            ],
            Prerequisites = [new SkillPrerequisite { SkillId = 42, RequiredNiveau = 2 }],
        };

        var bddCucumber = new SkillEntity
        {
            Id = 44,
            Name = "BDD & Cucumber / SpecFlow",
            Category = "Testing",
            Description = "Behaviour-Driven Development with Gherkin and Cucumber/SpecFlow.",
            LevelCount = 4,
            LevelDescriptors = [
                "Writes basic Gherkin scenarios",
                "Implements step definitions and hooks",
                "Collaborates with POs and devs on living documentation",
                "Coaches teams on the three amigos practice",
            ],
            Prerequisites = [new SkillPrerequisite { SkillId = 40, RequiredNiveau = 2 }],
        };

        var securityTesting = new SkillEntity
        {
            Id = 45,
            Name = "Security Testing (OWASP)",
            Category = "Testing",
            Description = "Applying OWASP Top 10, SAST/DAST tooling, threat modelling basics.",
            LevelCount = 3,
            LevelDescriptors = [
                "Aware of OWASP Top 10",
                "Uses automated SAST/DAST tools (OWASP ZAP, Snyk)",
                "Conducts threat modelling sessions",
            ],
        };

        var allSkills = new List<SkillEntity>
        {
            cleanCode, git, agile,
            csharp, aspNetCore, efCore, dotnetTesting, docker, designPatterns,
            javaFundamentals, springBoot, javaTesting, maven, microservicesJava, javaKafka,
            requirements, domainModelling, productOwnership, umlModelling, stakeholderMgmt, valueStream,
            manualTesting, testAutomation, apiTesting, performanceTesting, bddCucumber, securityTesting,
        };

        db.Skills.AddRange(allSkills);
        await db.SaveChangesAsync();

        // ── Profile mappings ─────────────────────────────────────────────────

        db.SkillProfiles.AddRange(
            // Universal skills in all profiles
            new SkillProfileEntity { SkillId = 1, Profile = CompetenceCentreProfile.DotNet, SortOrder = 1 },
            new SkillProfileEntity { SkillId = 2, Profile = CompetenceCentreProfile.DotNet, SortOrder = 2 },
            new SkillProfileEntity { SkillId = 3, Profile = CompetenceCentreProfile.DotNet, SortOrder = 3 },
            new SkillProfileEntity { SkillId = 1, Profile = CompetenceCentreProfile.Java, SortOrder = 1 },
            new SkillProfileEntity { SkillId = 2, Profile = CompetenceCentreProfile.Java, SortOrder = 2 },
            new SkillProfileEntity { SkillId = 3, Profile = CompetenceCentreProfile.Java, SortOrder = 3 },
            new SkillProfileEntity { SkillId = 3, Profile = CompetenceCentreProfile.POAnalysis, SortOrder = 1 },
            new SkillProfileEntity { SkillId = 3, Profile = CompetenceCentreProfile.QA, SortOrder = 1 },
            new SkillProfileEntity { SkillId = 2, Profile = CompetenceCentreProfile.QA, SortOrder = 2 },

            // .NET profile
            new SkillProfileEntity { SkillId = 10, Profile = CompetenceCentreProfile.DotNet, SortOrder = 4 },
            new SkillProfileEntity { SkillId = 11, Profile = CompetenceCentreProfile.DotNet, SortOrder = 5 },
            new SkillProfileEntity { SkillId = 12, Profile = CompetenceCentreProfile.DotNet, SortOrder = 6 },
            new SkillProfileEntity { SkillId = 13, Profile = CompetenceCentreProfile.DotNet, SortOrder = 7 },
            new SkillProfileEntity { SkillId = 14, Profile = CompetenceCentreProfile.DotNet, SortOrder = 8 },
            new SkillProfileEntity { SkillId = 15, Profile = CompetenceCentreProfile.DotNet, SortOrder = 9 },

            // Java profile
            new SkillProfileEntity { SkillId = 20, Profile = CompetenceCentreProfile.Java, SortOrder = 4 },
            new SkillProfileEntity { SkillId = 21, Profile = CompetenceCentreProfile.Java, SortOrder = 5 },
            new SkillProfileEntity { SkillId = 22, Profile = CompetenceCentreProfile.Java, SortOrder = 6 },
            new SkillProfileEntity { SkillId = 23, Profile = CompetenceCentreProfile.Java, SortOrder = 7 },
            new SkillProfileEntity { SkillId = 24, Profile = CompetenceCentreProfile.Java, SortOrder = 8 },
            new SkillProfileEntity { SkillId = 25, Profile = CompetenceCentreProfile.Java, SortOrder = 9 },

            // PO & Analysis profile
            new SkillProfileEntity { SkillId = 30, Profile = CompetenceCentreProfile.POAnalysis, SortOrder = 2 },
            new SkillProfileEntity { SkillId = 31, Profile = CompetenceCentreProfile.POAnalysis, SortOrder = 3 },
            new SkillProfileEntity { SkillId = 32, Profile = CompetenceCentreProfile.POAnalysis, SortOrder = 4 },
            new SkillProfileEntity { SkillId = 33, Profile = CompetenceCentreProfile.POAnalysis, SortOrder = 5 },
            new SkillProfileEntity { SkillId = 34, Profile = CompetenceCentreProfile.POAnalysis, SortOrder = 6 },
            new SkillProfileEntity { SkillId = 35, Profile = CompetenceCentreProfile.POAnalysis, SortOrder = 7 },

            // QA profile
            new SkillProfileEntity { SkillId = 40, Profile = CompetenceCentreProfile.QA, SortOrder = 3 },
            new SkillProfileEntity { SkillId = 41, Profile = CompetenceCentreProfile.QA, SortOrder = 4 },
            new SkillProfileEntity { SkillId = 42, Profile = CompetenceCentreProfile.QA, SortOrder = 5 },
            new SkillProfileEntity { SkillId = 43, Profile = CompetenceCentreProfile.QA, SortOrder = 6 },
            new SkillProfileEntity { SkillId = 44, Profile = CompetenceCentreProfile.QA, SortOrder = 7 },
            new SkillProfileEntity { SkillId = 45, Profile = CompetenceCentreProfile.QA, SortOrder = 8 });

        await db.SaveChangesAsync();

        // ── Seniority thresholds ─────────────────────────────────────────────

        db.SeniorityThresholds.AddRange(
            // ── .NET ─────────────────────────────────────────────────────────
            // Junior
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Junior, SkillId = 10, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Junior, SkillId = 2, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Junior, SkillId = 3, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Junior, SkillId = 13, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Junior, SkillId = 1, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Junior, SkillId = 11, MinNiveau = 1 },

            // Medior
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Medior, SkillId = 10, MinNiveau = 3 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Medior, SkillId = 11, MinNiveau = 3 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Medior, SkillId = 12, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Medior, SkillId = 13, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Medior, SkillId = 1, MinNiveau = 3 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Medior, SkillId = 14, MinNiveau = 1 },

            // Senior
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Senior, SkillId = 10, MinNiveau = 5 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Senior, SkillId = 11, MinNiveau = 5 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Senior, SkillId = 12, MinNiveau = 4 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Senior, SkillId = 13, MinNiveau = 4 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Senior, SkillId = 1, MinNiveau = 4 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.DotNet, SeniorityLevel = SeniorityLevel.Senior, SkillId = 15, MinNiveau = 3 },

            // ── Java ─────────────────────────────────────────────────────────
            // Junior
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Junior, SkillId = 20, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Junior, SkillId = 2, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Junior, SkillId = 3, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Junior, SkillId = 22, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Junior, SkillId = 23, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Junior, SkillId = 1, MinNiveau = 1 },

            // Medior
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Medior, SkillId = 20, MinNiveau = 3 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Medior, SkillId = 21, MinNiveau = 3 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Medior, SkillId = 22, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Medior, SkillId = 23, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Medior, SkillId = 1, MinNiveau = 3 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Medior, SkillId = 14, MinNiveau = 1 },

            // Senior
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Senior, SkillId = 20, MinNiveau = 5 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Senior, SkillId = 21, MinNiveau = 5 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Senior, SkillId = 22, MinNiveau = 4 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Senior, SkillId = 24, MinNiveau = 3 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Senior, SkillId = 1, MinNiveau = 4 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.Java, SeniorityLevel = SeniorityLevel.Senior, SkillId = 14, MinNiveau = 3 },

            // ── PO & Analysis ─────────────────────────────────────────────────
            // Junior
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Junior, SkillId = 30, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Junior, SkillId = 3, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Junior, SkillId = 33, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Junior, SkillId = 34, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Junior, SkillId = 32, MinNiveau = 1 },

            // Medior
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Medior, SkillId = 30, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Medior, SkillId = 31, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Medior, SkillId = 32, MinNiveau = 3 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Medior, SkillId = 3, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Medior, SkillId = 34, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Medior, SkillId = 33, MinNiveau = 2 },

            // Senior
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Senior, SkillId = 30, MinNiveau = 4 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Senior, SkillId = 31, MinNiveau = 4 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Senior, SkillId = 32, MinNiveau = 5 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Senior, SkillId = 34, MinNiveau = 4 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.POAnalysis, SeniorityLevel = SeniorityLevel.Senior, SkillId = 3, MinNiveau = 4 },

            // ── QA ────────────────────────────────────────────────────────────
            // Junior
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Junior, SkillId = 40, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Junior, SkillId = 3, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Junior, SkillId = 42, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Junior, SkillId = 44, MinNiveau = 1 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Junior, SkillId = 2, MinNiveau = 1 },

            // Medior
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Medior, SkillId = 40, MinNiveau = 3 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Medior, SkillId = 41, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Medior, SkillId = 42, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Medior, SkillId = 44, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Medior, SkillId = 3, MinNiveau = 2 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Medior, SkillId = 43, MinNiveau = 1 },

            // Senior
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Senior, SkillId = 41, MinNiveau = 4 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Senior, SkillId = 42, MinNiveau = 4 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Senior, SkillId = 43, MinNiveau = 3 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Senior, SkillId = 44, MinNiveau = 4 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Senior, SkillId = 40, MinNiveau = 4 },
            new SeniorityThresholdEntity { Profile = CompetenceCentreProfile.QA, SeniorityLevel = SeniorityLevel.Senior, SkillId = 3, MinNiveau = 3 });

        await db.SaveChangesAsync();
    }
}
