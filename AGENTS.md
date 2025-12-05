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

```
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

### Why bd?

- Dependency-aware: Track blockers and relationships between issues
- Git-friendly: Auto-syncs to JSONL for version control
- Agent-optimized: JSON output, ready work detection, discovered-from links
- Prevents duplicate tracking systems and confusion

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

### Issue Types

- `bug` - Something broken
- `feature` - New functionality
- `task` - Work item (tests, docs, refactoring)
- `epic` - Large feature with subtasks
- `chore` - Maintenance (dependencies, tooling)

### Priorities

- `0` - Critical (security, data loss, broken builds)
- `1` - High (major features, important bugs)
- `2` - Medium (default, nice-to-have)
- `3` - Low (polish, optimization)
- `4` - Backlog (future ideas)

### Workflow for AI Agents

1. **Check ready work**: `bd ready` shows unblocked issues
2. **Claim your task**: `bd update <id> --status in_progress`
3. **Work on it**: Implement, test, document
4. **Discover new work?** Create linked issue:
   - `bd create "Found bug" -p 1 --deps discovered-from:<parent-id>`
5. **Complete**: `bd close <id> --reason "Done"`
6. **Commit together**: Always commit the `.beads/issues.jsonl` file together with the code changes so issue state stays in sync with code state

### Auto-Sync

bd automatically syncs with git:
- Exports to `.beads/issues.jsonl` after changes (5s debounce)
- Imports from JSONL when newer (e.g., after `git pull`)
- No manual export/import needed!

### MCP Server (Optional)

If using Claude or MCP-compatible clients, install the beads MCP server:

```bash
pip install beads-mcp
```

Add to MCP config (e.g., `~/.config/claude/config.json`):
```json
{
  "beads": {
    "command": "beads-mcp",
    "args": []
  }
}
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
```
# AI planning documents (ephemeral)
history/
```

**Benefits:**
- ✅ Clean repository root
- ✅ Clear separation between ephemeral and permanent documentation
- ✅ Easy to exclude from version control if desired
- ✅ Preserves planning history for archeological research
- ✅ Reduces noise when browsing the project

### CLI Help

Run `bd <command> --help` to see all available flags for any command.
For example: `bd create --help` shows `--parent`, `--deps`, `--assignee`, etc.

### Session-Ending Protocol ("Landing the Plane")

When ending a session, you MUST complete ALL steps below. The session is NOT complete until `git push` succeeds. NEVER stop before pushing. NEVER say "ready to push when you are!" - that is a FAILURE.

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

**4. PUSH TO REMOTE - NON-NEGOTIABLE**
```bash
# Pull first to catch any remote changes
git pull --rebase

# If conflicts in .beads/issues.jsonl, resolve thoughtfully:
#   - git checkout --theirs .beads/issues.jsonl (accept remote)
#   - bd import -i .beads/issues.jsonl (re-import)
#   - Or manual merge, then import

# Sync the database
bd sync

# MANDATORY: Push everything to remote
git push

# MANDATORY: Verify push succeeded
git status  # MUST show "up to date with origin"
```

**5. Clean up git state**
```bash
git stash clear                    # Remove old stashes
git remote prune origin            # Clean up deleted remote branches
```

**6. Verify clean state**
- All changes committed AND PUSHED
- No untracked files remain
- `git status` shows clean working tree

**7. Provide handoff for next session**
```
## Next Session Context
- Current branch: <branch-name>
- Ready work: `bd ready` shows <N> issues
- In progress: <issue-id> - <description>
- Blockers: <any issues or questions>

Recommended prompt: "Continue work on <issue-id>: [issue title]. [Brief context]"
```

**CRITICAL RULES:**
- The session is NOT complete until `git push` succeeds
- NEVER stop before `git push` - that leaves work stranded locally
- NEVER say "ready to push when you are!" - YOU must push, not the user
- If `git push` fails, resolve the issue and retry until it succeeds

### Before Committing

1. **Run tests**: `dotnet test` (at minimum for changed projects)
2. **Build check**: `dotnet build` for affected projects
3. **Update docs**: If you changed behavior, update relevant README or docs
4. **Sync issues**: `bd sync` to ensure issue state matches code state

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

- Check existing issues: `bd list --json`
- Look at recent commits: `git log --oneline -20`
- Read the docs in `docs/` directory
- Create an issue if unsure: `bd create "Question: ..." -t task -p 2`

### Important Rules

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
- ❌ Do NOT clutter repo root with planning documents
- ❌ Do NOT use ASCII art for diagrams (use Mermaid instead)
