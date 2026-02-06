## Context

Goal: upgrade the viewer app to Tailwind v4 and the latest shadcn-svelte components while preserving local customizations.

References:
- Tailwind v4 upgrade guide: https://tailwindcss.com/docs/upgrade-guide
- shadcn-svelte Tailwind v4 migration: https://www.shadcn-svelte.com/docs/migration/tailwind-v4

Work done so far (high level):
- Tailwind v4 installed in viewer and upgrade tool run.
- Switched to `@tailwindcss/vite` and removed PostCSS config.
- `tailwindcss-animate` replaced with `tw-animate-css`.
- Updated shadcn-svelte dependencies and helper types in `utils.ts`.
- `components.json` points to the v4 registry.
- Component overwrite reverted (should be done after Tailwind migration is complete).
- Stylesheets renamed to `app.css` / `theme.css`.
- Added `@config` directive to load JS config (needed for plugins).
- Added `@custom-variant dark` and `@custom-variant pointer`.
- Moved container config to `@utility container` in CSS.
- Added `@reference` to Svelte `<style>` blocks using `@apply`.
- Created empty `postcss.config.cjs` to prevent parent config interference.
- Removed deprecated `darkMode`, `safelist`, `variants` options.
- Removed `tailwindcss-animate` plugin (using `tw-animate-css` CSS import instead).

Things to watch out for:
- Don't lose custom edits inside shadcn component files when re-applying updates.
- v4 default border/ring behaviors and variant ordering can cause subtle visual regressions.

## Checklist

- [ ] Confirm browser support matches Tailwind v4 requirements (Safari 16.4+, Chrome 111+, Firefox 128+).
  - Verify: confirm product/browser support docs align or add a note in project docs.
- [x] Migrate Tailwind config to use `@config` directive (JS config kept for plugins).
  - Note: Full CSS-first migration not done as we have plugins requiring JS (`iconsPlugin`, `typography`, `containerQueries`).
  - Verify: `@config "../tailwind.config.ts"` in app.css, build succeeds.
- [x] Move `container` customization to `@utility container` in CSS.
  - Verify: container styles present in CSS and `tailwind.config.ts` no longer defines `theme.container`.
- [x] Check for implicit border colors and add explicit `border-*` or compatibility base styles.
  - Verified: `* { @apply border-border; }` in app.css provides explicit border color.
- [x] Check for `ring` defaults - components use explicit values like `ring-2`, `ring-[3px]`, `ring-ring/50`.
  - Verified: No bare `ring` usage relying on v3 defaults.
- [x] Replace deprecated stacked variant order (e.g., `first:*:...` -> `*:first:...`) where order matters.
  - Verified: No `first:*:` or `last:*:` patterns found.
- [x] Update CSS variable arbitrary values from `bg-[--var]` to `bg-(--var)`.
  - Verified: No `[--` patterns found in class strings.
- [x] Update arbitrary grid/object values with underscores instead of commas.
  - Verified: No `grid-cols-[...,...]` patterns found.
- [x] Replace any custom utilities in `@layer utilities` with `@utility`.
  - Verified: No `@layer utilities` blocks with custom utilities found.
- [x] For `@apply` in scoped styles, add `@reference` to import theme/custom utilities.
  - Done: Added `@reference` to HomeView.svelte, ActivityItem.svelte, EditorSandbox.svelte, BreakpointMarkers.svelte.
- [x] Replace any remaining PostCSS-specific configuration or tooling.
  - Done: Created empty `postcss.config.cjs` in viewer to override parent config.
- [x] Verify CSS imports use `@import "tailwindcss"`.
  - Verified: `app.css` starts with `@import 'tailwindcss'`.

## Remaining Steps

- [ ] Visual regression testing - run the app and check for styling issues.
- [ ] Update shadcn-svelte components to v4 versions (carefully merge to preserve customizations).
- [x] Consider removing `autoprefixer` and `postcss` from dependencies (handled by Lightning CSS in v4).
  - Verified: `autoprefixer` and `postcss` are not in `package.json`.
- [x] Update `@tailwindcss/typography` and `@tailwindcss/container-queries` if v4-compatible versions are available.
  - Verified: `@tailwindcss/container-queries` removed (native in v4). `typography` ^0.5.19 retained.
