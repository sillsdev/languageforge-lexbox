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

**Before saying done or committing**, run `task verify` from the repo root once (svelte-check, eslint, vitest unit, i18n extract; 3 to 7 min for both apps). `src/project/demo/demo-project-view.test.ts` mounts the whole demo app there, so a component that breaks the demo fails fast instead of in 60+ Playwright tests.

### Dev server: one per worktree

Vite and Playwright default to port 5173, and Playwright **reuses whatever already listens there**: another checkout's server gives stale code and a false pass. Per worktree, set `FwLite__DevAssetsPort` to a free port before `pnpm run dev` or `task test:ui-standalone`; `vite.config.ts`, the Playwright config and FwLiteWeb all follow it. Check first with `netstat -ano | findstr :5173` and stop stray vite processes. EACCES on a port means Windows reserved it (`netsh interface ipv4 show excludedportrange protocol=tcp`); pick another.

### Browser pane (Claude preview tools)

- Start the viewer with `preview_start` (config `viewer` in `.claude/launch.json`, port 5173); `navigate` to localhost is denied. With `FwLite__DevAssetsPort` set, vite binds that port instead.
- Screenshots need the pane displayed; otherwise use `read_page` / `get_page_text`. Call `read_page` before `find`.
- Wrap `javascript_tool` snippets in an IIFE; top-level `const` collides across calls.
- `left_click_drag` times out on paneforge resize handles: use the keyboard or set sizes via JS.

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
| Manual | `**/*.manual.test.ts` | ⚠️ Opt-in — `pnpm test:manual`. Hits the network, so it is excluded from `pnpm test` and CI |

**Don't run E2E or Launcher tests unless you've explicitly set up that infrastructure** — they fail loudly without it and the setup isn't part of normal dev.

UI tests (the runnable ones) — from `frontend/viewer/`:

```bash
# Demo project: http://localhost:5173/testing/project-view
# Filter to a file and one browser (the right choice for specific changes); --ui for UI mode
task test:ui-standalone -- sort.test.ts --project=chromium
```

UI tests run under `chromium` and `webkit` (Safari's engine, on Linux); unfiltered, every file runs twice.
WebKit snapshots use `-webkit`-suffixed Argos baselines; chromium keeps the bare names.

- **Selectors**: locate by role, `data-testid`, or `data-field-id`. Selectors on serialized styles (`[style*="grid-area:"]`) broke 38 WebKit tests.
- **Demo knobs** (`window.__PLAYWRIGHT_UTILS__`, set in `src/project/demo/in-memory-demo-api.ts`): `demoApi`, `setWrite(bool)`, `setHasHardwareKeyboard(bool)`. TODO: no knob yet for the comments feature, unread counts, or the user's role; add one to the demo instead of patching components.

### Known noise (pre-existing)

- `ResizeObserver loop completed with undelivered notifications` in the vite log: harmless, already filtered by `src/lib/errors/global-errors.ts`.
- WebKit `test.skip`s citing #2678 (virtualized scroll, image reload timing): known; leave them.

### Theme (light/dark + color) for screenshots

Theming is `mode-watcher`. Don't click the `ThemePicker` popover — set it directly.

**Light/dark mode** defaults to `system` (follows `prefers-color-scheme`), so emulate that media query:
- Playwright: `await page.emulateMedia({colorScheme: 'dark'})` (or `'light'`). For both, use the `assertScreenshotInBothColorSchemes` helper.
- Browser MCP: pass `colorScheme: 'dark'` to `resize_window`.

(Only breaks if something first called `setMode`, which persists a preference that overrides system.)

**Color theme** (`green`/`blue`/`rose`/`orange`/`violet`/`stone`; `blue` is the default) has no media query — set the `data-theme` attribute on `<html>` instead:
- `document.documentElement.setAttribute('data-theme','violet')` (Browser MCP `javascript_tool`, or Playwright `page.evaluate`).
- To survive a reload, set `localStorage['mode-watcher-theme']` before load.

## Project Structure

| Path | Purpose |
|------|---------|
| `src/AppRoutes.svelte` | Routes (`svelte-routing`) |
| `src/lib/` | Shared components, entry editor |
| `src/locales/` | i18n catalogs (`.po`) |
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

Extraction skips `gt` in `<script module>` (those strings never reach `en.po`): call `gt` inside the instance `<script>` or the markup.

## Adding Components

```bash
# Add ShadCN component
npx shadcn-svelte@next add context-menu
```

## Error Handling

Unexpected errors should reach the global error handler (`src/lib/errors/global-errors.ts`): let them
throw (or rethrow) rather than catching and rendering raw messages inline. It shows a persistent toast
with a copy-error button and logs to .NET. Inline UI error states are for *expected*, actionable
failures (offline, not-found, retry) with plain, translated messages.

## Feature flags

Frontend-only release channels live in `src/lib/feature-flags/`. Users type a
channel in Troubleshoot (empty = production). Gate preview UI with
`hasFlag('flag-name')` or `<FlagContent flag="flag-name">`. Flag names are typed
from `CHANNEL_FLAGS`. Map flags onto preview channels only; when a feature
ships, delete the flag — production has none. The `dev` channel is special:
`DevContent` shows and `hasFlag` is always true. Do not list `dev` or
`production` in `CHANNEL_FLAGS`.

## Services

- `useXService()` / `useService(key)` throw when the service isn't registered, and each host registers a different set (the demo only what `in-memory-demo-api.ts` and `browser-app-services.ts` set). In an always-mounted component resolve lazily, at use time, or with `tryUseService`; a top-level call once took down the viewer and 62 Playwright tests.
- `[JSInvokable]` goes on the C# interface and the concrete class. A new MiniLcm JSInvokable method needs a stub in `src/project/demo/in-memory-demo-api.ts`.
- Judgement calls (dev-only state in dev settings, gate UI on `features.*`, config flag over new abstraction): root `CODING_STANDARDS.md`.

## Important Files

- `lingui.config.ts` - i18n configuration
- `components.json` - ShadCN-svelte config
- `src/lib/entry-editor/` - Entry editing components
- `src/lib/feature-flags/` - Release-channel feature flags (`hasFlag`, `FlagContent`)
