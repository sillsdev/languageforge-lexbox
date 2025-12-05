# Frontend (LexBox Web App)

SvelteKit application for the LexBox web interface. Handles project management, user authentication, organization admin, and project browsing.

> **Note**: `frontend/viewer/` is a **separate app** - the FwLite dictionary editor UI. See `viewer/AGENTS.md`.

## Development

```bash
# Install dependencies
pnpm install

# Start dev server (localhost:3000)
pnpm run dev

# Build for production
pnpm run build

# Run tests
pnpm test

# Lint (requires build first)
pnpm run -r build
pnpm run -r lint
```

## Tech Stack

- **Framework**: SvelteKit
- **UI Library**: DaisyUI (Tailwind-based)
- **Data**: GraphQL via AJAX to backend
- **Auth**: JWT cookie (http-only)
- **Package Manager**: pnpm

## Project Structure

| Path | Purpose |
|------|---------|
| `src/routes/` | SvelteKit routes (file-based routing) |
| `src/lib/` | Shared components, utilities, stores |
| `src/lib/icons/` | Icon components (various sources) |
| `tests/` | Playwright E2E tests |
| `viewer/` | **Separate app** - FwLite editor (see `viewer/AGENTS.md`) |

## Code Patterns

### GraphQL

- Schema at `schema.graphql`
- Codegen: `pnpm run gql-codegen`
- Queries in `src/lib/gql/`

### Components

- Use DaisyUI classes for consistent styling
- Icons from `$lib/icons/`
- Stores in `$lib/stores/`

### Testing

- Playwright for E2E tests
- Test files in `tests/`
- Run with `pnpm test`

## Important Files

- `svelte.config.js` - SvelteKit configuration
- `vite.config.ts` - Vite bundler config
- `schema.graphql` - GraphQL schema (generated from backend)
- `src/routes/+layout.svelte` - Root layout
- `src/hooks.server.ts` - Server hooks (auth, etc.)
