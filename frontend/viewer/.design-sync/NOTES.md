# design-sync notes (FW Lite viewer)

## Why this is off-script

The `/design-sync` converter targets **React** design systems: it esbuild-bundles the
repo's compiled `dist/` into `_ds_bundle.js` and ships `.jsx`/`.d.ts` so claude.ai/design's
design agent can instantiate the real components. FW Lite's viewer is **Svelte + Tailwind v4
(ShadCN-Svelte)**, so there is no consumable React runtime bundle. This sync therefore ships a
**design-token + visual-reference** system produced by hand:

- Real tokens/themes/fonts (all 6 themes × light/dark) reachable from `styles.css`'s `@import` closure.
- Hand-authored HTML preview cards (`@dsCard` first line) for the core primitives, styled with the
  app's **real compiled Tailwind CSS** (captured from the running dev server / `vite build`), not a
  reimplementation.
- A conventions header (`.design-sync/conventions.md`) teaching the shadcn class idiom so agent
  output is on-brand.

No `_ds_bundle.js`, no `_vendor/`, no `.d.ts` (those are React-runtime artifacts). No `_ds_sync.json`
anchor is written (the storybook/package hash recipes don't apply to a hand-authored Svelte layout);
each re-sync re-verifies, which is the honest/safe default here.

## Source facts
- Tokens: `src/theme.css` (OKLCH). Themes: default(blue)/green/rose/orange/violet/stone via `[data-theme=...]`; light via `:root,.light`, dark via `.dark`.
- Fonts: Noto Sans (`--font-sans`), Inter (`.font-inter`); loaded via `vite-plugin-webfont-dl` (inlined into webfonts.css at build).
- Class idiom: Tailwind v4 utilities + shadcn semantic colors (`bg-primary`, `text-foreground`, `border-border`, `bg-muted`, ...); radius scale from `--radius`.
- Compiled CSS source: dev server on :5173 (vite) and :5137 (FwLiteWeb host).
