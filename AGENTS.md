# Agent Instructions for languageforge-lexbox

## Project Overview

This is a monorepo containing:
- **LexBox** - A web application for managing linguistic data (backend in C#/.NET, frontend in Svelte)
- **FwLite** - A lightweight FieldWorks application (MAUI desktop app)
- **FwHeadless** - A headless service for FieldWorks data processing

## Tech Stack

- **Backend**: .NET 9, C#, Entity Framework Core, GraphQL (Hot Chocolate)
- **Frontend**: SvelteKit, TypeScript
- **Database**: PostgreSQL
- **Infrastructure**: Docker, Kubernetes, Skaffold, Tilt

## Development Commands

```bash
# Backend
dotnet build backend/LexBoxApi/LexBoxApi.csproj
dotnet test

# FwLite (Windows)
dotnet build backend/FwLite/FwLiteMaui/FwLiteMaui.csproj --framework net9.0-windows10.0.19041.0

# Frontend
cd frontend && pnpm dev
```

## Project Structure

```text
languageforge-lexbox/
├── backend/
│   ├── LexBoxApi/       # Main API (ASP.NET Core + GraphQL)
│   ├── LexCore/         # Core domain models
│   ├── LexData/         # Data access layer (EF Core)
│   ├── FwLite/          # FwLite MAUI app
│   ├── FwHeadless/      # Headless FW service
│   └── Testing/         # Test projects
├── frontend/            # SvelteKit web app
└── deployment/          # K8s/Docker configs
```

**IMPORTANT: Testing Policy**
- ❌ **Do NOT run integration tests** (`dotnet test`) unless the user explicitly asks
- Integration tests require full test infrastructure (database, services) and take significant time
- Only run unit tests locally when verifying critical business logic
- User must explicitly request test runs before executing them

### Checking GitHub Issues and PRs

When asked to check GitHub issues or PRs, use the `gh` CLI instead of browser tools:

```bash
# List open issues
gh issue list --limit 30

# List open PRs
gh pr list --limit 30

# View specific issue or PR
gh issue view <number>
gh pr view <number>
```

Provide an in-conversation summary highlighting:
- Urgent/critical issues (regressions, bugs, broken builds)
- Common themes or patterns
- Items needing immediate attention

**Why CLI over browser**: Faster, less tokens, easier to scan and discuss.

### Important Files

Key documentation for this project:
- `README.md` - Project overview and setup
- `AGENTS.md` - You are here! Agent instructions
- `.github/copilot-instructions.md` - GitHub Copilot auto-loaded instructions
- `.github/AGENTS.md` - **CI/CD and deployment guide** (workflows, K8s, Docker)
- `docs/DEVELOPER-win.md` - Windows development setup
- `docs/DEVELOPER-linux.md` - Linux development setup
- `docs/DEVELOPER-osx.md` - macOS development setup
- `backend/README.md` - Backend architecture
- `backend/FwLite/AGENTS.md` - **FwLite/CRDT critical code guide** (data loss risks!)
- `backend/FwHeadless/AGENTS.md` - **FwHeadless sync guide**
- `deployment/README.md` - Deployment and infrastructure

### Questions?

- Check existing issues: `gh issue list --limit 30`
- Look at recent commits: `git log --oneline -20`
- Read the docs in `docs/` directory
- Create a GitHub issue if unsure

### Important Rules

- ✅ Use GitHub Issues for task tracking
- ✅ Use `gh` CLI for GitHub issues/PRs, not browser tools
- ✅ Use **Mermaid diagrams** for flowcharts and architecture (not ASCII art)
- ✅ Do NOT run integration tests unless user explicitly requests
- ❌ Do NOT use ASCII art for diagrams (use Mermaid instead)
- ❌ Do NOT git commit or git push without explicit user approval
