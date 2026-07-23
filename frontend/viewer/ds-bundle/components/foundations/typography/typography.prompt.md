# Typography

**Fonts**: `--font-sans` = **Noto Sans** (default; broad script coverage for linguistic data), `--font-mono` = monospace, and **Inter** available via the `.font-inter` utility.

**Scale** (Tailwind): `text-xs` (captions/badges), `text-sm` (secondary/muted), `text-base` (body/inputs), `text-lg`–`text-2xl` (headings). **Weights**: `font-normal | font-medium | font-semibold | font-bold`.

Fonts are loaded via the Google Fonts `@import` at the top of the stylesheet (inlined at production build by `vite-plugin-webfont-dl`).
