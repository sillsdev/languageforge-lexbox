# GitHub Copilot Instructions for languageforge-lexbox

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

## Important Rules

- ✅ Use GitHub Issues for task tracking
- ✅ Use `gh` CLI for GitHub issues/PRs, not browser tools
- ✅ Use **Mermaid diagrams** for flowcharts and architecture (not ASCII art)
- ❌ Do NOT use ASCII art for diagrams (use Mermaid instead)

---

**For detailed workflows and advanced features, see [AGENTS.md](../AGENTS.md)**

**For critical code areas:**
- [FwLite/CRDT Guide](../backend/FwLite/AGENTS.md) - Data sync, model changes (HIGH RISK)
- [FwHeadless Guide](../backend/FwHeadless/AGENTS.md) - Mercurial sync, FwData processing
- [CI/CD Guide](./AGENTS.md) - Workflows, deployments, K8s
