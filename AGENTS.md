# Agent Instructions for languageforge-lexbox

## Project Overview

This is a monorepo containing:
- **LexBox** - A web application for managing linguistic data (backend in C#/.NET, frontend in Svelte)
- **FwLite** - A lightweight FieldWorks application (MAUI desktop app)
- **FwHeadless** - A headless service for FieldWorks data processing

### Tech Stack

- **Backend**: .NET 10, C#, Entity Framework Core, GraphQL (Hot Chocolate)
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

- ✅ **DO run unit tests via the CLI**, filtered to the tests relevant to your changes (e.g. `dotnet test backend/FwLite/FwLiteShared.Tests --filter "FullyQualifiedName~MyTestClass"`). Verify tests you wrote or changed actually pass before handing work back. Never run whole suites just to "see if anything broke".
- ✅ **`MiniLcm.Tests`, `LcmCrdt.Tests`, `FwDataMiniLcmBridge.Tests` need no infrastructure** — these are the unit/conformance tests for the two `IMiniLcmApi` implementations; run them filtered to your change like any unit test (this is where a `*TestsBase` change should be verified, including its FwData side). The FwData/LCM-backed selections load a real project (a handful of tests = seconds, a whole class = a minute or two), so keep the filter tight; don't reach for the whole class or project.
- ✅ **FwLite integration tests** (e.g. `FwLiteProjectSync.Tests`) need no infrastructure but are slow. FwData runs **in-process via liblcm** — these tests create real FieldWorks projects with no external setup (e.g. the `ProjectTemplateTests.GenerateTemplate` regen tool). Only FwHeadless's hg/Mercurial sync and the lexbox stack (last bullet) need running infrastructure — *not* FwData itself. Run a **targeted selection** (specific tests, not necessarily whole classes) when you touched critical sync code **and believe the work is finished** — not on every iteration. Waiting on tests burns time; be deliberate about which runs buy real signal.
- ✅ **`backend/Testing` contains unit tests too** — only tests marked `Category=Integration|FlakyIntegration|RequiresDb` (and the `Testing.Browser` namespace) need infrastructure. Its unit tests are fine to run: `task test:unit -- <filter>` excludes those categories for you.
- ✅ **FwLite viewer Playwright tests MAY be run** — they're cheap: `task test:ui-standalone -- <test-name-filter>` (from `frontend/viewer/`) auto-starts the vite dev server with the in-browser demo project; no lexbox stack, chromium only. Always filter to relevant tests; details in `frontend/viewer/AGENTS.md`.
- ❌ **Do NOT run tests that need the lexbox stack** unless the user explicitly asks: LexBox integration tests (`Category=Integration`/`FlakyIntegration`, `Testing.Browser`) and the lexbox frontend Playwright suite (`frontend/tests`). The local stack is usually down or torn down between sessions and results aren't trustworthy — rely on CI for these.

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
- ✅ When pulling PR comments with `gh` use `api`. It's the only thing that returns review comments.
- ✅ If the user asks about "the" PR, but does not explicitly name a PR or branch, assume they mean the PR associated with the current branch.
- ✅ Use **Mermaid diagrams** for flowcharts and architecture (not ASCII art)
- ✅ Prefer IDE diagnostics (compiler/lint errors) over CLI tools for identifying issues. Fixing these diagnostics is part of completing any instruction.
- ✅ When handling a user prompt ALWAYS ask for clarification if there are details to clarify, important decisions that must be made first or the plan sounds unwise
- ❌ Do NOT git commit or git push without explicit user approval
- ⚠️ **Pre-commit hook prettier-formats staged `*.{js,ts,svelte,css,md,json}` files** (yes, incl. `.md`/docs) via `frontend/viewer`. If your commit stages any of those and `frontend/viewer/node_modules` is missing (fresh worktree/clone), run `cd frontend && pnpm install` first (~45s, once per worktree) — don't `--no-verify` to dodge it, that just defers the format failure to CI. If it stages none of those (e.g. backend-only), the hook is a no-op: it passes with no install, and `--no-verify` is fine.

### 🛡️ VIGILANCE

- ❌ **NEVER "fix" a failure** by removing assertions, commenting out code, or changing data to match a broken implementation.
- ✅ **ALWAYS fix the root cause** when a test or check fails.
- ✅ **ALWAYS double-check** that your "fix" hasn't made a check or test meaningless (e.g., asserting `expect(true).toBe(true)`).
- ✅ **Assert that E2E test user actions** e.g. (scroll, click, etc.) actually have the expected effect before proceeding further.

If you are struggling, explain the difficulty to the user instead of cheating. **Integrity is non-negotiable.**

## Cursor Cloud specific instructions

System deps (.NET 10 SDK, Taskfile/`task`) are baked into the VM snapshot; the startup update script only refreshes pnpm + FwLiteWeb NuGet deps. The generated TS types under `frontend/viewer/src/lib/dotnet-types/generated-types/` are committed, so the viewer runs without a backend build.

### Running FW Lite Web (headless)

`task fw-lite-web` is the documented dev workflow but it also launches the https-proxy + Storybook (only needed for MSAL OAuth / component dev). For headless cloud testing, run just the two core processes (each in its own background/tmux session):

- Viewer (Vite dev server, port 5173): `pnpm -C frontend/viewer run dev`
- Backend host (port 5137): `cd backend/FwLite/FwLiteWeb && dotnet run`

The app is at `http://localhost:5137` (Blazor host that serves the viewer via Vite dev assets). On the home page, click **Create Example Project** → open it to test entry editing fully offline — no remote login required.

### Gotcha: embedded webview

`FwLiteWeb/appsettings.Development.json` sets `OpenBrowser: true`, which spawns an embedded Photino/Chromium window on startup. In headless this floods logs with harmless `dbus`/`gpu` errors and is unusable. When testing through an external browser, start the backend with `FwLiteWeb__OpenBrowser=false dotnet run` to suppress it (runtime override, no file change).
