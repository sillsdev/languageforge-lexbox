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
├── deployment/          # K8s/Docker configs
└── .beads/              # Issue tracking database
```

## Issue Tracking with bd (beads)

**IMPORTANT**: This project uses **bd (beads)** for ALL issue tracking. Do NOT use markdown TODOs, task lists, or other tracking methods.

### Quick Start

**Check for ready work:**
```bash
bd ready --json
```

**Create new issues:**
```bash
bd create "Issue title" -t bug|feature|task -p 0-4 --json
bd create "Issue title" -p 1 --deps discovered-from:bd-123 --json
bd create "Subtask" --parent <epic-id> --json  # Hierarchical subtask (gets ID like epic-id.1)
```

**Claim and update:**
```bash
bd update bd-42 --status in_progress --json
bd update bd-42 --priority 1 --json
```

**Complete work:**
```bash
bd close bd-42 --reason "Completed" --json
```

### Workflow for AI Agents

1. **Check ready work**: `bd ready` shows unblocked issues
2. **Claim your task**: `bd update <id> --status in_progress`
3. **Work on it**: Implement, test, document
4. **Discover new work?** Create linked issue:
   - `bd create "Found bug" -p 1 --deps discovered-from:<parent-id>`
5. **Complete**: `bd close <id> --reason "Done"`
6. **Commit together**: Always commit the `.beads/issues.jsonl` file together with the code changes so issue state stays in sync with code state

### Priorities

- `0` - Critical (security, data loss, broken builds)
- `1` - High (major features, important bugs)
- `2` - Medium (default, nice-to-have)
- `3` - Low (polish, optimization)
- `4` - Backlog (future ideas)

### MCP Server (Optional)

If using Claude or MCP-compatible clients, install the beads MCP server:

```bash
pip install beads-mcp
```

Then use `mcp__beads__*` functions instead of CLI commands.

### Managing AI-Generated Planning Documents

AI assistants often create planning and design documents during development:
- PLAN.md, IMPLEMENTATION.md, ARCHITECTURE.md
- DESIGN.md, CODEBASE_SUMMARY.md, INTEGRATION_PLAN.md
- TESTING_GUIDE.md, TECHNICAL_DESIGN.md, and similar files

**Best Practice: Use a dedicated directory for these ephemeral files**

**Recommended approach:**
- Create a `history/` directory in the project root
- Store ALL AI-generated planning/design docs in `history/`
- Keep the repository root clean and focused on permanent project files
- Only access `history/` when explicitly asked to review past planning

**Example .gitignore entry (optional):**
```gitignore
# AI planning documents (ephemeral)
history/
```

**Benefits:**
- ✅ Clean repository root
- ✅ Clear separation between ephemeral and permanent documentation
- ✅ Easy to exclude from version control if desired
- ✅ Preserves planning history for archeological research
- ✅ Reduces noise when browsing the project

## CLI Help

Run `bd <command> --help` to see all available flags for any command.
For example: `bd create --help` shows `--parent`, `--deps`, `--assignee`, etc.

## Session-Ending Protocol ("Landing the Plane")

When ending a session, you MUST complete ALL steps below. The session is NOT complete until `git push` succeeds, but do not push until the human has verified the changes and requests the session to be ended/the plane to be landed.

**MANDATORY WORKFLOW - COMPLETE ALL STEPS:**

**1. File issues for remaining work**
- Create issues for any discovered bugs, TODOs, or follow-up tasks
- Use `bd create "..." -t bug|task|feature -p 0-4 --json`

**2. Update issue status**
- Close completed issues: `bd close <id> --reason "Done" --json`
- Update in-progress work: `bd update <id> --status in_progress --json`

**3. Run quality gates (if code was changed)**
```bash
dotnet build backend/LexBoxApi/LexBoxApi.csproj
dotnet test
# File P0 issues if builds are broken
```

**4. PAUSE HERE and WAIT for user to verify and sign off before moving on!**

E.g. by asking "Are you ready to move on?"
User can jump here or confirm by e.g. "Landing the plane"

**5. Push to remote**
```bash
# Pull first to catch any remote changes
git pull --rebase

# If conflicts in .beads/issues.jsonl, resolve thoughtfully:
#   - git checkout --theirs .beads/issues.jsonl (accept remote)
#   - bd import -i .beads/issues.jsonl (re-import)
#   - Or manual merge, then import

# Sync the database
bd sync

# Push everything to remote
git push

# Verify push succeeded
git status  # MUST show "up to date with origin"
```

**6. Verify clean state**
- All changes committed AND PUSHED
- No untracked files remain
- `git status` shows clean working tree

**7. Provide handoff for next session**
```markdown
## Next Session Context
- Current branch: <branch-name>
- Ready work: `bd ready` shows <N> issues
- In progress: <issue-id> - <description>
- Blockers: <any issues or questions>

Recommended prompt: "Continue work on <issue-id>: [issue title]. [Brief context]"
```

## Important Rules

- ✅ Use bd for ALL task tracking
- ✅ Always use `--json` flag for programmatic use
- ✅ Link discovered work with `discovered-from` dependencies
- ✅ Check `bd ready` before asking "what should I work on?"
- ✅ Store AI planning docs in `history/` directory
- ✅ Run `bd <cmd> --help` to discover available flags
- ✅ Follow session-ending protocol before finishing work
- ✅ Use `gh` CLI for GitHub issues/PRs, not browser tools
- ✅ Use **Mermaid diagrams** for flowcharts and architecture (not ASCII art)
- ❌ Do NOT create markdown TODO lists
- ❌ Do NOT use external issue trackers
- ❌ Do NOT duplicate tracking systems
- ❌ Do NOT clutter repo with planning documents, they should be in dedicated `history/` directory
- ❌ Do NOT use ASCII art for diagrams (use Mermaid instead)

---

**For detailed workflows and advanced features, see [AGENTS.md](../AGENTS.md)**

**For critical code areas:**
- [FwLite/CRDT Guide](../backend/FwLite/AGENTS.md) - Data sync, model changes (HIGH RISK)
- [FwHeadless Guide](../backend/FwHeadless/AGENTS.md) - Mercurial sync, FwData processing
- [CI/CD Guide](./AGENTS.md) - Workflows, deployments, K8s
