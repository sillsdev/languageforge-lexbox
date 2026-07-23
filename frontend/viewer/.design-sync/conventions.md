# FW Lite design system

FW Lite is the **FieldWorks Lite dictionary editor** — a SvelteKit app styled with **Tailwind CSS v4** and **ShadCN-Svelte**. Its audience is linguists editing dictionary data, so the UI favors clarity, generous hit targets, and dense multi-language forms.

> The real components are **Svelte**, but the entire look is expressed through **portable design tokens + Tailwind utility classes**. Reproduce FW Lite's appearance by using the exact token utilities below — never hardcode colors, and never invent arbitrary values that aren't in the stylesheet.

## Theme setup (required)

All color comes from CSS variables that live on a theme root. Every screen must:

1. Put a theme class on a root wrapper: `.light` (default) or `.dark`. The app toggles `.dark` at runtime (`mode-watcher`).
2. Paint the root surface with `bg-background text-foreground`.
3. Optionally select one of six **accent themes** via `data-theme="green|rose|orange|violet|stone"` (default/blue = no attribute). **Every design must look correct in both light and dark.**

```html
<div class="light bg-background text-foreground">…</div>
<div class="dark bg-background text-foreground">…</div>
```

## Styling idiom — semantic Tailwind utilities

Use the semantic color pairs, not raw palette colors:

| Purpose | Utilities |
|---|---|
| Page / surface | `bg-background` · `text-foreground` |
| Card / panel | `bg-card` · `text-card-foreground` · `bg-popover` |
| Primary action | `bg-primary` · `text-primary-foreground` |
| Secondary | `bg-secondary` · `text-secondary-foreground` |
| Muted / hints | `bg-muted` · `text-muted-foreground` |
| Hover / subtle | `bg-accent` · `text-accent-foreground` |
| Danger | `bg-destructive` · `text-destructive-foreground` |
| Lines / fields | `border-border` · `border-input` · `ring-ring` |

**Radius** (from `--radius`): `rounded-md` (controls/buttons/inputs), `rounded-xl` (cards), `rounded-full` (badges/avatars).
**Type**: `font-sans` = Noto Sans (default), `.font-inter` (alternate), `font-mono`; sizes `text-xs → text-2xl`; weights `font-medium | font-semibold`.
**Icons**: Material Design Icons via classes like `i-mdi-plus`, `i-mdi-check`, sized with `size-4`.

## Component recipes (from the real components)

- **Button**: `inline-flex items-center justify-center gap-2 rounded-md text-sm font-medium h-10 px-4 py-2 shadow-xs transition-all` + a variant pair — `bg-primary text-primary-foreground hover:bg-primary/90` (default), `bg-secondary …`, `bg-destructive text-white`, `border border-input bg-background hover:bg-accent` (outline), `hover:bg-accent` (ghost), `text-primary hover:underline` (link).
- **Input**: `border-input bg-background h-10 w-full rounded-md border px-3 py-2 text-base md:text-sm shadow-xs focus-visible:ring-ring/50 focus-visible:ring-[3px] focus-visible:border-ring`.
- **Badge**: `inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium` + variant colors.
- **Card**: `bg-card text-card-foreground flex flex-col gap-6 rounded-xl border py-6 shadow-sm`; sections use `px-6`.
- **Multi-writing-system field** (FW Lite's signature): a field title above rows of `writing-system code label → input`. Dictionary/entry fields are almost always multi-writing-system — prefer this over a single input for any linguistic content.

## Layout patterns

Compose screens from FW Lite's real structure, not generic page templates:

- **App shell** — a `bg-sidebar` sidebar (project switcher, primary `New Word` action, nav group with an active/badge state, footer utilities) beside a `bg-background` primary view. Active nav item = `bg-sidebar-accent text-sidebar-accent-foreground font-medium`.
- **Master-detail** — a fixed-width list pane (filter bar + entry rows: headword / muted gloss / part-of-speech badge; selected row `bg-accent`) beside a flexible detail pane, split by a resizable divider. Side-by-side ≥801px; single-pane with push navigation below.

See the `Layouts` cards + their `prompt.md` for the responsive rules.

## Where the truth lives

- **Tokens + compiled utilities**: `styles.css` → `tokens/app-compiled.css` (the real compiled Tailwind output; all 6 themes × light/dark, the shadcn color layer, fonts, utilities). Read it before styling.
- **Per-component API + recipe**: each `components/<group>/<Name>/<Name>.prompt.md`.

## Example

```html
<div class="light bg-background text-foreground">
  <div class="bg-card text-card-foreground flex flex-col gap-4 rounded-xl border p-6 shadow-sm">
    <div class="text-sm font-semibold">Lexeme form</div>
    <div style="display:grid;grid-template-columns:3.25rem 1fr;gap:.5rem;align-items:baseline">
      <label class="text-sm font-medium text-muted-foreground" style="text-align:right">Eng</label>
      <input class="border-input bg-background h-10 w-full rounded-md border px-3 py-2 text-sm shadow-xs" value="dog">
    </div>
    <button class="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground shadow-xs hover:bg-primary/90">Save</button>
  </div>
</div>
```
