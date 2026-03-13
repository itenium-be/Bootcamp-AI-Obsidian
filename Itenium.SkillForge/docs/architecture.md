# SkillForge — Architecture Baseline

## Overview

SkillForge is a **Learning Management System (LMS)** built for itenium. It supports three user tiers (BackOffice, Manager, Learner) with team-scoped access to courses and learning content.

---

## System Architecture

```
┌─────────────────────────────────────────────────────────┐
│                      Browser                            │
│   React 19 + TypeScript  (Vite, port 5173)              │
│   TanStack Router · React Query · Zustand · Tailwind    │
└──────────────────────┬──────────────────────────────────┘
                       │ HTTP / REST (axios + bearer token)
                       │ OAuth 2.0  /connect/token
                       ▼
┌─────────────────────────────────────────────────────────┐
│                  .NET 10 WebAPI  (port 5000)             │
│   ASP.NET Core · OpenIddict · EF Core 10                │
│   Swagger  ·  Health Checks  ·  Serilog                 │
└──────────────────────┬──────────────────────────────────┘
                       │ Npgsql
                       ▼
┌─────────────────────────────────────────────────────────┐
│              PostgreSQL 17  (port 5432)                  │
│              Docker container: skillforge-db             │
└─────────────────────────────────────────────────────────┘
```

---

## Backend Layer Diagram

```
Itenium.SkillForge.WebApi          ← HTTP entry point, DI composition root
  └── Controllers/                 ← Route handlers, request/response models
        CourseController           ← CRUD: /api/course
        TeamController             ← GET:  /api/team

Itenium.SkillForge.Services        ← Business logic, cross-cutting concerns
  └── ISkillForgeUser              ← Current user abstraction (claims-based)
      SkillForgeUser               ← JWT claim extraction
      Capability (enum)            ← Fine-grained permissions

Itenium.SkillForge.Data            ← Data access layer
  └── AppDbContext                 ← EF Core context (extends ForgeIdentityDbContext)
      SeedData                     ← Test users, roles, teams, courses
      Migrations/                  ← EF migrations (auto-applied at startup)

Itenium.SkillForge.Entities        ← Domain models (plain C# classes)
  └── CourseEntity                 ← id, name, description, category, level, createdAt
      TeamEntity                   ← id, name (CompetenceCenter)
```

### Key Backend Dependencies

| Package | Purpose |
|---|---|
| `Itenium.Forge` (v0.3.x) | Shared library: security, OpenIddict, health checks, logging, Swagger |
| `EF Core 10` + `Npgsql` | ORM + PostgreSQL provider |
| `OpenIddict` | OAuth 2.0 / OIDC server (token endpoint, Identity integration) |
| `NUnit` + `NSubstitute` | Unit + integration testing |
| `Testcontainers` | Spins up real PostgreSQL for tests |

---

## Frontend Layer Diagram

```
main.tsx                      ← Bootstrap: QueryClient, Router, i18n, Toaster

routes/
  __root.tsx                  ← Root layout (Outlet + Sonner toaster)
  (auth)/sign-in.tsx          ← Public: OAuth login form
  _authenticated/             ← Protected layout (redirects if not authed)
    index.tsx → Dashboard     ← /
    courses.tsx               ← /courses
    settings.tsx              ← /settings

pages/
  Dashboard.tsx               ← Stats overview
  Courses.tsx                 ← Course listing (React Query)
  Settings.tsx                ← User settings (placeholder)
  SignIn.tsx                  ← Login form + test user quick-fill

components/
  Layout.tsx                  ← App shell: sidebar, nav, theme, language

stores/
  authStore.ts                ← Zustand: JWT, user info, isAuthenticated
  teamStore.ts                ← Zustand: mode (backoffice/manager), selectedTeam
  themeStore.ts               ← Zustand: light/dark theme

api/
  client.ts                   ← Axios instance, token injection, login, API calls

i18n/
  en.json / nl.json           ← Translations (default: nl, fallback: en)
```

### Key Frontend Dependencies

| Package | Purpose |
|---|---|
| `@tanstack/react-router` v1 | File-based routing with auth guards |
| `@tanstack/react-query` v5 | Server state, caching (10s stale time) |
| `zustand` v5 | Client state with localStorage persistence |
| `axios` | HTTP client with interceptors |
| `react-hook-form` + `zod` | Form handling and validation |
| `react-i18next` | EN/NL internationalization |
| `@itenium-forge/ui` | Shared component library |
| `tailwindcss` v4 | Utility-first styling |
| `sonner` | Toast notifications |
| `vitest` + `@playwright/test` | Unit + E2E testing |

---

## Authentication & Authorization

```
User ──POST /connect/token──► OpenIddict ──► JWT Access Token (60 min)
                                          └─► Refresh Token (14 days)

JWT Claims:
  sub        → user ID
  email      → user email
  name       → display name
  role       → backoffice | manager | learner
  team       → [java, dotnet, ...]   (manager only, multi-value)

Authorization model:
  backoffice → all resources
  manager    → team-scoped (filtered by "team" claim)
  learner    → read-only
```

### Capabilities (fine-grained permissions)

| Capability | Roles |
|---|---|
| `ReadCourse` | all authenticated users |
| `ManageCourse` | backoffice, manager |

---

## Database Schema

```
Teams          id, name
Courses        id, name, description, category, level, createdAt

-- ASP.NET Identity (via ForgeIdentityDbContext)
AspNetUsers             id, email, name, ...
AspNetRoles             id, name
AspNetUserRoles         userId, roleId
AspNetUserClaims        userId, claimType, claimValue  ← team claims here

-- OpenIddict
OpenIddictApplications
OpenIddictTokens
OpenIddictAuthorizations
OpenIddictScopes
```

---

## Infrastructure

### Docker Compose

```yaml
postgres:
  image: postgres:17
  container: skillforge-db
  port: 5432
  database: skillforge / skillforge / skillforge
  volume: postgres_data (persistent)
```

### Environments

| Setting | Development | Docker |
|---|---|---|
| DB Host | `localhost` | `postgres` |
| Frontend URL | `http://localhost:5173` | — |
| API URL | `http://localhost:5000` | — |
| Token endpoint | `/connect/token` | same |

---

## Testing Strategy

| Layer | Framework | Notes |
|---|---|---|
| Backend unit | NUnit + NSubstitute | Service-level logic |
| Backend integration | NUnit + Testcontainers | Real PostgreSQL, transaction rollback per test |
| Frontend unit | Vitest + JSDOM | Component and store tests |
| E2E | Playwright + Testcontainers | Full stack; local or Docker backend |

---

## Key Design Decisions

1. **Layered architecture** — Entities → Data → Services → WebApi; no circular dependencies
2. **OpenIddict** — standards-compliant OAuth 2.0 server embedded in the API (no external IdP needed)
3. **Claim-based team scoping** — user claims drive data filtering, enabling multi-team managers without role explosion
4. **Zustand with persistence** — lightweight client state; auth and team selection survive page refreshes
5. **React Query** — server state separated from client state; automatic background refetching
6. **File-based routing** — TanStack Router auto-generates route tree; route guards via `_authenticated` layout
7. **Centralized NuGet packages** — `Directory.Packages.props` enforces consistent versions across all backend projects
8. **Migrations auto-applied** — no manual `dotnet ef database update` needed in dev/prod

---

## Planned / In-Progress

Based on the sidebar navigation defined in `Layout.tsx`, the following features are planned but not yet implemented:

- **Learner features**: My Courses, My Progress, My Certificates
- **Manager features**: Team Members, Team Progress, Assignments, Course Management
- **BackOffice features**: User Admin, Team Admin, Reports (Usage, Completion, Feedback)
- **Course-Team assignment**: linking courses to teams
- **Enrollment / tracking**: learner progress and certificates

---

## Test Users

| Username | Password | Role | Teams |
|---|---|---|---|
| backoffice | AdminPassword123! | backoffice | all |
| java | UserPassword123! | manager | Java |
| dotnet | UserPassword123! | manager | .NET |
| multi | UserPassword123! | manager | Java + .NET |
| learner | UserPassword123! | learner | — |
