# Decide how hg and FwLite history are presented together

Type: prototype
Status: open
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
