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
| `tests/` | Playwright E2E tests |

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

## Testing UI Components

### Demo Project & In-Memory API

The viewer includes an in-memory demo API for local testing without a backend:

```bash
# Start dev server (http://localhost:5174)
pnpm run dev

# Access the demo project at:
# http://localhost:5174/testing/project-view/browse
```

The demo project contains 1464 sample entries from Swahili. Used for testing:
- Virtual scrolling behavior
- Entry filtering and search
- Selection persistence
- Large list performance

### Chrome MCP for UI Testing

Use the Chrome MCP server to test interactive UI features not easily covered by Storybook:

**Start dev server in background (required for CLI-based testing):**

```bash
# Windows - run in background (required)
start pnpm run dev

# Then wait a few seconds for server to start, then use Chrome MCP
mcp_chrome__navigate_page "http://localhost:5174/testing/project-view/browse"
mcp_chrome__fill "[selector]" "search text"
mcp_chrome__click "[selector]"
mcp_chrome__take_snapshot  # For visual verification
```

**When to use Chrome MCP:**
- Testing multi-step user flows (search → click → verify detail panel updates)
- Verifying scroll behavior in virtual lists
- Checking that state persists across navigations
- Testing filter/sort interactions with selection

**Not for Chrome MCP:**
- Simple component rendering → Use Storybook
- Unit logic → Use Vitest
- E2E workflows with real backend → Use Playwright in `tests/`

### Running Tests Locally

```bash
# Unit tests
pnpm test

# Storybook (visual component testing)
pnpm run storybook

# E2E tests (requires backend or demo mode)
pnpm run playwright
```
