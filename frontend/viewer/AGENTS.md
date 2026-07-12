# Frontend Viewer (FwLite Web UI)

SvelteKit application for the **FwLite dictionary editor**. This is the web UI for editing linguistic data with CRDT-based real-time sync.

> **Note**: This is a **separate app** from the parent `frontend/` (LexBox web). Different purpose, different stack choices.

## Development

```bash
# Typical workflow (from repo root)
task fw-lite-web

# Or manually:
pnpm install
pnpm run dev
```

### Generated .NET Types

This project depends on TypeScript types and API interfaces generated from .NET (via `Reinforced.Typings`). If you change .NET models or `JSInvokable` APIs, you must rebuild the backend to update these types.

```bash
# From repo root
dotnet build backend/FwLite/FwLiteShared/FwLiteShared.csproj

# Verify types are committed (also runs in CI):
task fw-lite:has-stale-generated-types
```

The generated files are located in `src/lib/dotnet-types/generated-types/`.

### Testing

| Suite | Location | Runnable here? |
|---|---|---|
| UI | `tests/ui/` | ✅ Yes — auto-starts a dev server with in-memory demo; no infra needed |
| E2E | `tests/e2e/` | ❌ Needs a Lexbox kind cluster + published FwLiteWeb binary |
| Launcher | `tests/launcher/` | ❌ Needs a published FwLiteWeb binary |

**Don't run E2E or Launcher tests unless you've explicitly set up that infrastructure** — they fail loudly without it and the setup isn't part of normal dev.

UI tests (the runnable ones) — from `frontend/viewer/`:

```bash
# Dev server / demo project: http://localhost:5173/testing/project-view (useful for Chrome MCP debugging).

# Filter by test name (the ONLY RIGHT choice when testing specific features or changes), e.g.
task test:ui-standalone -- entries-list

# All UI tests
task test:ui-standalone

# Playwright UI mode
task test:ui-standalone -- entries-list --ui
```

## Tech Stack

- **Framework**: SvelteKit + Vite
- **UI**: ShadCN-Svelte (Tailwind-based)
- **i18n**: Lingui (`svelte-i18n-lingui`)
- **State**: CRDT sync with backend MiniLcm API
- **Testing**: Playwright + Vitest

## Project Structure

| Path | Purpose |
|------|---------|
| `src/routes/` | SvelteKit routes |
| `src/lib/` | Shared components, entry editor |
| `src/locales/` | i18n translation files (JSON) |
| `.storybook/` | Component storybook |
| `tests/` | Playwright (UI + e2e) and Vitest (launcher) tests |

## i18n (Lingui)

```svelte
<span>{$t`Logout`}</span>
<span>{$t`Hello ${name}`}</span>
```

```bash
# Extract strings for translation
pnpm run i18n:extract
```

Add new language: Edit `lingui.config.ts`, then run extract.

## Adding Components

```bash
# Add ShadCN component
npx shadcn-svelte@next add context-menu
```

## Key Concepts

- **MiniLcm**: Lightweight dictionary API (entries, senses, definitions)
- **CRDT Sync**: Real-time sync via Yjs/Harmony
- **Entry Editor**: Main editing interface for dictionary entries

## Important Files

- `lingui.config.ts` - i18n configuration
- `components.json` - ShadCN-svelte config
- `src/lib/entry-editor/` - Entry editing components
- `src/routes/` - Page routes
