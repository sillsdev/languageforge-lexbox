# AGENTS.md

This file provides guidance to AI agents when working with code in this repository.

## What this is

A Platform.Bible (aka, `paranext-core`) extension (named `lexicon`) that provides lexicon management UI for Bible translation projects. It launches and communicates with a FieldWorks Lite (FW Lite) .NET backend process via HTTP REST.

## Setup

`paranext-core` must be cloned as a sibling directory (e.g., `../paranext-core`). Several `devDependencies` resolve to local paths there (`papi-dts`, `platform-bible-react`, `platform-bible-utils`).

Install:

```bash
npm core:install
npm install
```

Build FW Lite backend (required before running):

```bash
task build-fw-lite
```

## Commands

| Command                     | Purpose                                                       |
| --------------------------- | ------------------------------------------------------------- |
| `npm start`                 | Run Platform.Bible with this extension (watch + core start)   |
| `npm run build`             | Build once (dev)                                              |
| `npm run watch`             | Rebuild on file changes                                       |
| `npm run build:production`  | Production build                                              |
| `npm run package`           | Production build + zip for distribution                       |
| `npm run lint`              | ESLint + stylelint                                            |
| `npm run lint-fix`          | Format + lint with auto-fix                                   |
| `npm run format`            | Prettier format                                               |
| `npm run core:copy-package` | Copy built package to paranext-core (for cross-extension dev) |

> **Warning:** Use `npm`, not `pnpm`.

There are no automated tests in this project.

## Architecture

### Extension lifecycle (`src/main.ts`)

On activation, the extension:

1. Launches the FW Lite process on `http://localhost:29348`
2. Registers 5 WebView providers (Main, AddWord, FindWord, FindRelatedWords, SelectLexicon)
3. Registers `lexicon.entryService` as a PAPI network object so other extensions can call it
4. Registers commands (`addEntry`, `browseLexicon`, `displayEntry`, `findEntry`, `findRelatedEntries`, `lexicons`, `selectLexicon`)
5. Creates a `ProjectManagers` instance to manage per-project state

### Key abstractions

- **`src/utils/fw-lite-api.ts`** — HTTP client wrapping FW Lite's REST API. All lexicon data reads and writes go through this.
- **`src/services/entry-service.ts`** — Implements `IEntryService`; exposed as a PAPI network object. Other extensions can call this via `networkObjects.get<IEntryService>('lexicon.entryService')`.
- **`src/utils/project-manager.ts`** / **`project-managers.ts`** — Per-project state (lexicon code, analysis language, language tag, active WebView IDs). All functionality is scoped to a Paratext project ID.
- **`src/types/lexicon.d.ts`** — Public TypeScript types for external consumers (`IEntry`, `ISense`, `IEntryQuery`, `IEntryService`, `PartialEntry`, etc.).
- **`src/web-views/`** — Top-level React components registered as WebView providers with PAPI. Each `*.web-view.tsx` file is bundled separately by webpack.
- **`src/components/`** — Shared React components used across WebViews.

### Build pipeline

Webpack runs two passes: WebViews first (`webpack/webpack.config.web-view.ts`), then main (`webpack/webpack.config.main.ts`). SWC is used for fast transpilation. Tailwind CSS is processed via PostCSS. Output goes to `dist/`.

### PAPI patterns

- Import from `'@papi/core'` for types, `'@papi/backend'` in main, `'@papi/frontend'` in web-views.
- WebViews receive project context (e.g., project ID) via props from the platform.
- `experimental decorators` are enabled in tsconfig for PAPI usage.

## Contributions

Menus, settings, and localized strings are declared in `contributions/` and referenced by `manifest.json`. The extension requires the `createProcess` elevated privilege to launch FW Lite.
