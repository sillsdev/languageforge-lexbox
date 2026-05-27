# FwLite Viewer — conventions checklist

Read this in full before filing findings on any `frontend/viewer/**` diff.

Authoritative AGENTS.md: `frontend/viewer/AGENTS.md`. This file encodes the
review *taste* — the rules the team reaches for most often.

## The #1 rule: no `try/catch` around async handlers

The viewer has a global error handler (toast + clipboard + dotnet log)
that catches rejected promises uniformly. Adding `try/catch` or
`.catch(...)` around async defeats it for that path — the error is
swallowed. Stated firmly on PR #2215: *"those are handled more elegantly
by our generic global error handler."*

**Exception:** the catch sets specific form-level UI state (not a console
log). That stays.

Grep `frontend/viewer/**` for `} catch` adjacent to `await`, and
`\.catch\(`. Match → blocking unless it sets specific UI state. Fix is
to delete the catch.

## Svelte 5 runes — use them, don't mix old patterns

The viewer is on Svelte 5.x. New code must use runes:

| Use | Don't |
|---|---|
| `let foo = $state(0)` | `let foo = 0` (no reactivity) |
| `let bar = $derived(foo * 2)` | `$: bar = foo * 2` |
| `$effect(() => { … })` | `$: { … }` |
| `let { prop } = $props()` | `export let prop` |
| `let { val = $bindable() } = $props()` | `export let val` + `bind:val` consumer hack |

Snippets (`{#snippet name()}…{/snippet}` + `{@render name()}`) replace
slots. Don't use `<slot>` in new code.

Grep for `export let` or `^\$:` in newly-touched `.svelte` files → flag as
**important** (mixing patterns is hard to maintain).

## State / context patterns

- Services live in `*.svelte.ts` files (e.g. `save-event-service.svelte.ts`,
  `entry-loader-service.svelte.ts`).
- Type-safe context via `runed`'s `Context` class:
  `export const subjectContext = new Context<EditorPrimitiveSubject>('subject-context');`
- `initXxx()` to set, `useXxx()` to consume.
- Local component state: `$state`. Derived: `$derived`. Cleanup: `$effect`
  return function.

## i18n — Lingui

- Strings via `$t\`text\`` or `msg\`text\``.
- After adding strings, run `pnpm run i18n:extract` (in `frontend/viewer/`).
- Add context comments per `frontend/viewer/I18N_CONTEXT_GUIDE.md`:
  - Mark Classic vs. Lite view applicability.
  - Describe UI element location.
  - Note variable content.
  - Cross-reference equivalent terms.
- Don't import from `svelte-intl-precompile` — use `$lib/i18n` (ESLint
  enforces).

Grep for new string literals in JSX/templates → ask: should this be
internationalized? If user-visible, yes.

### Parser awareness

Lingui extracts strings via static analysis at build time. It only sees
strings the parser actually traverses — runtime-only string assembly is
invisible. From PR #1829: structure role/label data so each translatable
phrase is a literal at extract time, not assembled at runtime.

```typescript
// bad: parser doesn't see "Manager" / "Editor" as i18n strings
const role = isManager ? 'Manager' : 'Editor';
const label = $t`Role: ${role}`;     // role substitutes an untranslated string

// good: structure as data; each label is a literal the parser can extract
const roles = [
  { value: 'manager', label: msg`Manager` },
  { value: 'editor',  label: msg`Editor` },
];
```

If a translatable string is constructed (concatenation, conditional, fill
of an untranslated value), the parser won't extract it — translators
never see it.

## Generated .NET types

The viewer consumes types generated from .NET via Reinforced.Typings.

If the diff touches `backend/FwLite/FwLiteShared/**` or any `.cs` class with
`[TsInterface]` or similar attributes:

- Run `dotnet build backend/FwLite/FwLiteShared/FwLiteShared.csproj`.
- The generated files live in
  `frontend/viewer/src/lib/dotnet-types/generated-types/`.
- Check that the regenerated files are in the diff. If they're not but the
  .NET types changed → **blocking** (the viewer's TS types are stale).

## Naming

- Components: `PascalCase.svelte`.
- Folders / utility files: `kebab-case`.
- Function names describe what they actually do. PR #2215 caught
  `filterAndSortWs` that didn't sort — renamed to `filterWs`. If a function
  name claims more than the body delivers → **important**.
- Type-only exports use `export type`.

## Component composition

- One component per file (no `.svelte` + sibling `.ts` split).
- Public types exported from `<script module>` block.
- Compound components (e.g. `Button + XButton + CopyButton`) re-exported
  via `index.ts`.
- Prefer snippets over props with HTML strings.
- Generic components: `<script lang="ts" generics="T">`.

## Styling

- Tailwind utility classes (`@tailwindcss/vite`).
- Custom breakpoints documented in `tailwind.config.ts` (`xs-form`,
  `sm-view`, etc.).
- Avoid `style=""` attributes unless dynamic.
- Icons: `@lucide/svelte` or `@mdi/js` (via `@egoist/tailwindcss-icons`).

## Tests

- E2E: Playwright tests in `frontend/viewer/tests/*.test.ts`.
- Unit: Vitest, with browser project for component tests.
- Stories: `src/stories/**/*.stories.svelte` with optional `play` functions
  for vitest-integrated tests.
- Adding a new user-facing interaction → expect a Playwright test (PR #2295
  added `tab-focus-preservation.test.ts`). Pure visual tweak → tests
  optional, screenshots in PR body expected.

## Test commands

| When | Command | Time |
|---|---|---|
| Quick check after edits | `pnpm --filter viewer run check` (from `frontend/`) | ~30 s |
| Format | `pnpm --filter viewer run format` | ~20 s |
| Unit | `pnpm --filter viewer run test:unit` | ~15 s |
| E2E (single test) | `task playwright-test-standalone -- entries-list` (auto-starts dev server) | ~1 min |
| E2E (full) | `task playwright-test-standalone` | ~3 min |
| i18n extract | `pnpm --filter viewer run i18n:extract` | ~5 s |

