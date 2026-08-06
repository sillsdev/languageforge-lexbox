# Assemble the implementation-ready spec

Type: task
Status: resolved
Blocked by: 01, 02, 03

## Question

Fold the resolved decisions (on-demand hg UX + bounding, FwLite server API shape +
transport, combined layout) into a single spec `/implement` can build from.

The spec should name, concretely:
- Frontend changes: the trigger/component changes in `+page.svelte` / `+page.ts`,
  reuse-or-replace of `HgLogView`, and the new FwLite list component.
- Backend changes: the new FwLite-history query/endpoint over `CrdtCommits`
  (GraphQL field on `Project` or REST), its DTO, ordering, and any bounding/paging.
- The relevance gating (`hasHarmonyCommits`) and the empty/only-one-source cases.
- Test surface: which existing tests move, what new coverage is needed.

Blocked until 01–03 are resolved. If the user has asked the map to carry the build,
split this into task-type build tickets instead of a single spec doc.

Output: the spec (linked asset or committed doc), map marked done.

## Answer

Spec written: [SPEC.md](../SPEC.md). It folds the three resolved decisions into a
build plan — new `Project.harmonyCommits` GraphQL field + `HarmonyCommit` DTO over
`CrdtCommits` (with the server-side `Metadata`-is-a-serialized-string caveat called
out), gating both hg and FwLite fetches behind independent per-section "Show" toggles,
a new `HarmonyLogView.svelte`, the presence/empty matrix, i18n + codegen steps, and a
test surface. Ready to hand to `/implement`.

Map is complete — no tickets remain.
