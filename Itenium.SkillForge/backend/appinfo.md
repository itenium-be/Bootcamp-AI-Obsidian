● SkillForge is a Learning Management System (LMS) for managing skill development in organizations.

  ---
  Tech Stack

  Frontend
  - React 19 + TypeScript, Vite
  - TanStack Router (file-based routing) + React Query
  - Zustand (auth, team, theme state)
  - Tailwind CSS 4, Lucide icons, Sonner toasts
  - React Hook Form + Zod, i18next (NL/EN)
  - Vitest + Playwright for testing

  Backend
  - .NET 10 ASP.NET Core Web API
  - Entity Framework Core + PostgreSQL 17
  - OpenIddict (OAuth/OIDC + JWT auth)
  - Serilog, xUnit + Testcontainers

  ---
  What it does

  Three roles with different access:

  ┌────────────┬──────────────────────────────────────────────────────────────────────┐
  │    Role    │                                Can do                                │
  ├────────────┼──────────────────────────────────────────────────────────────────────┤
  │ Learner    │ Browse catalog, enroll in courses, track progress, earn certificates │
  ├────────────┼──────────────────────────────────────────────────────────────────────┤
  │ Manager    │ Manage team members, assign courses, view team analytics             │
  ├────────────┼──────────────────────────────────────────────────────────────────────┤
  │ Backoffice │ Admin: manage users, teams, courses, view reports                    │
  └────────────┴──────────────────────────────────────────────────────────────────────┘

  ---
  Project Structure

  Itenium.SkillForge/
  ├── frontend/          # React app
  ├── backend/
  │   ├── WebApi/        # Controllers, Program.cs
  │   ├── Data/          # EF Core DbContext, migrations, seed data
  │   ├── Entities/      # CourseEntity, TeamEntity
  │   └── Services/      # User context, authorization capabilities
  └── docker-compose.yml # PostgreSQL 17

  It's a bootcamp project — a realistic LMS scaffold using private Itenium packages (@itenium-forge/ui, NuGet packages) for shared UI components and
  infrastructure.