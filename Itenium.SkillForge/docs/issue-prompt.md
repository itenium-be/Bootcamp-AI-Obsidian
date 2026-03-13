# SkillForge — Issue Solving Prompt

## Context

You are working on **SkillForge**, an LMS built for itenium.

**Stack:**
- Frontend: React 19 + TypeScript, Vite (port 5173), TanStack Router, React Query, Zustand, Tailwind v4
- Backend: .NET 10 WebAPI (port 5000), ASP.NET Core, EF Core 10, OpenIddict, PostgreSQL 17
- Package manager: `bun` (not npm/yarn)
- Testing: NUnit + Testcontainers (backend), Vitest + Playwright (frontend)

**Architecture:** Layered — Entities → Data → Services → WebApi. Read `docs/architecture.md` for the full picture before making any changes.

---

## The Issue

**Issue title:** [PASTE TITLE]
**Issue description:**
```
[PASTE FULL ISSUE DESCRIPTION]
```

**Relevant labels / acceptance criteria:** [e.g. backend, frontend, bug, feature]

---

## Instructions

1. Read `docs/architecture.md` before writing any code.
2. Write tests **first** (TDD — red/green). Backend: NUnit. Frontend: Vitest.
3. Implement the feature/fix following the layered architecture.
4. Before finishing, run:
   - `bun run lint && bun run typecheck && bun run test`
   - `dotnet format && dotnet test`
5. Ensure Docker is running (`docker compose up -d`) before starting the backend.

**Do not** introduce circular dependencies between layers. **Do not** bypass auth guards or capability checks.

---

## Deliverables

- [ ] Tests written and passing
- [ ] Implementation complete
- [ ] Lint + typecheck + tests all green
- [ ] Brief summary of what was changed and why
