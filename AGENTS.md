# Agent Instructions for languageforge-lexbox

## Project Overview

This is a monorepo containing:
- **LexBox** - A web application for managing linguistic data (backend in C#/.NET, frontend in Svelte)
- **FwLite** - A lightweight FieldWorks application (MAUI desktop app)
- **FwHeadless** - A headless service for FieldWorks data processing

### Tech Stack

- **Backend**: .NET 9, C#, Entity Framework Core, GraphQL (Hot Chocolate)
- **Frontend**: SvelteKit, TypeScript
- **Database**: PostgreSQL
- **Infrastructure**: Docker, Kubernetes, Skaffold, Tilt

### Structure

```text
languageforge-lexbox/
├── backend/
│   ├── LexBoxApi/       # Main API (ASP.NET Core + GraphQL)
│   ├── LexCore/         # Core domain models
│   ├── LexData/         # Data access layer (EF Core)
│   ├── FwLite/          # FwLite MAUI app
│   ├── FwHeadless/      # Headless FW service
│   └── Testing/         # Test projects
├── frontend/            # Lexbox SvelteKit web app
├── frontend/viewer/     # FieldWorks Lite frontend Svelte code
└── deployment/          # K8s/Docker configs
```

### Important Files

Key documentation for this project:
- `README.md` - Project overview and setup
- `AGENTS.md` - You are here! Agent instructions
- `.github/AGENTS.md` - **CI/CD and deployment guide** (workflows, K8s, Docker)
- `docs/DEVELOPER-win.md` - Windows development setup
- `docs/DEVELOPER-linux.md` - Linux development setup
- `docs/DEVELOPER-osx.md` - macOS development setup
- `backend/README.md` - Backend architecture
- `backend/AGENTS.md` - General backend guidelines
- `backend/LexBoxApi/AGENTS.md` - API & GraphQL specific rules
- `backend/FwLite/AGENTS.md` - **FwLite/CRDT** (Critical code! Data loss risks!)
- `backend/FwHeadless/AGENTS.md` - **FwHeadless guide** (Critical code! Data loss risks! Mercurial sync, FwData processing)
- `frontend/AGENTS.md` - General frontend/SvelteKit rules
- `frontend/viewer/AGENTS.md` - **FwLite Viewer** (Specific frontend rules)
- `deployment/README.md` - Deployment and infrastructure

## Guidelines

### Testing

- ❌ **Do NOT run dotnet INTEGRATION tests** unless the user explicitly asks. They require full test infrastructure (database, services) which usually isn't available.
- ✅ **DO run unit tests locally** and filter to the tests that are relevant to the changes you are making. Use IDE testing tools over the cli.

### Questions?

- Check existing issues: `gh issue list --limit 30`
- Look at recent commits: `git log --oneline -20`
- Read the docs in `docs/` directory
- Create a GitHub issue if unsure
- Ask the user to clarify

### Pre-Flight Check

Before implementing any change that will touch many files or is in a 🔴 **Critical** area (FwLite sync, FwHeadless) do a "Pre-Flight Check" and list every component in the chain that will be touched (e.g., MiniLcm -> LcmCrdt -> FwDataBridge -> SyncHelper).

### Important Rules

- ✅ **ALWAYS read local `AGENTS.md` files** in the directories you are working in (and their parents) before starting.
- ✅ **ALWAYS review relevant code paths** before asking clarification questions.
- ✅ New instructions in AGENTS.md files should be SUCCINCT.
- ✅ Use `gh` CLI for GitHub issues/PRs, not browser tools
- ✅ Use **Mermaid diagrams** for flowcharts and architecture (not ASCII art)
- ✅ Prefer IDE diagnostics (compiler/lint errors) over CLI tools for identifying issues. Fixing these diagnostics is part of completing any instruction.
- ✅ Do NOT run integration tests unless user explicitly requests
- ✅ When handling a user prompt ALWAYS ask for clarification if there are details to clarify, important decisions that must be made first or the plan sounds unwise
- ❌ Do NOT git commit or git push without explicit user approval

### 🛡️ VIGILANCE

- ❌ **NEVER "fix" a failure** by removing assertions, commenting out code, or changing data to match a broken implementation.
- ✅ **ALWAYS fix the root cause** when a test or check fails.
- ✅ **ALWAYS double-check** that your "fix" hasn't made a check or test meaningless (e.g., asserting `expect(true).toBe(true)`).
- ✅ **Assert that E2E test user actions** e.g. (scroll, click, etc.) actually have the expected effect before proceeding further.

If you are struggling, explain the difficulty to the user instead of cheating. **Integrity is non-negotiable.**
