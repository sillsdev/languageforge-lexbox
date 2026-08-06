# Assemble the implementation-ready spec

Type: task
Status: open
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
