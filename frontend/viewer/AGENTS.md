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
