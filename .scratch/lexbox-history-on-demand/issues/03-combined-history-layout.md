# Decide how hg and FwLite history are presented together

Type: prototype
Status: resolved
Blocked by: 01, 02

## Question

When a project has both Mercurial history and FwLite/CRDT history (`hasHarmonyCommits`
== true), how are the two shown? And when only one is relevant, what shows?

Candidates:
- **Two separate lists/sections** — each loaded on demand independently, clearly
  labelled by source. Simplest; no identity/time reconciliation.
- **One merged chronological timeline** — interleaved, tagged by source. Needs the
  author-identity reconciliation noted in the map's fog (hg `user` string vs CRDT
  `AuthorId` GUID) and consistent timestamps.

Also: for a non-FwLite project (no harmony commits), the page shows only hg history —
confirm the FwLite section is simply absent, not an empty state.

Blocked because the shape depends on the on-demand trigger (01) and the FwLite entry
shape/transport (02).

Output: decided layout (rough `/prototype` if useful), including the both/either/only
cases and whether the two on-demand triggers are independent or unified.

## Answer

**Two separate, labeled sections — not a merged timeline.** Stacked on the project
page:
1. **History** (existing hg section, keeps `HgLogView`'s commit graph) — stays where
   it is now, first.
2. **FieldWorks Lite history** (new) — a flat list below it, rendered from
   `project.harmonyCommits` (ticket 02): author name + timestamp per row.

Deciding constraint: `HgLogView` renders a commit graph (train-tracks + rev/parents),
while FwLite rows are flat, message-less, and capped — a single interleaved table
can't hold both cleanly, and merging a capped list into an unbounded one leaves
confusing gaps. Two sections sidestep all of it.

**Independent per-section on-demand toggles.** Each section has its own "Show" button:
- hg keeps ticket 01's collapsed + "Show history" trigger (unbounded fetch on click).
- FwLite gets its own collapsed + "Show FieldWorks Lite history" trigger (capped
  fetch on click). Opening the cheap capped FwLite list never triggers the expensive
  hg fetch, and vice versa.

**Presence / empty cases:**
- FwLite section is **rendered only when `project.hasHarmonyCommits` is true** — for a
  non-FwLite project it's simply absent (no empty state).
- hg section behaves exactly as today (present for projects with a repo).
- No project shows a merged view; the two are always visually distinct.

No prototype artifact needed — decision made from concrete options grounded in the
real rendering constraints.

Resolves the map's "author identity reconciliation" fog: since the histories are
**never merged**, hg `user` strings and CRDT author names never need reconciling.
