<!-- wayfinder:map -->
# Map: On-demand history on the LexBox project page

## Destination

An implementation-ready design for how the LexBox project page presents history:
Mercurial history loads **on demand** (not on every page visit), and **FieldWorks
Lite (CRDT) history** can be shown alongside it **when relevant** (`hasHarmonyCommits`).
Initial form is deliberately minimal — a **list of commits** with author name and
timestamp (no graph, no diffs, no drill-down). The map is done when every open
decision below is resolved and a spec exists that `/implement` can build from.

## Notes

- **Tracker**: local markdown (this `.scratch/lexbox-history-on-demand/` dir), per user.
- **Mode**: planning. Tickets resolve *decisions*; the final ticket assembles the
  spec. Execution is handed to `/implement` afterward — unless the user asks the map
  to carry the build (then ticket 04 becomes task-type build tickets).
- **Skills**: `/grilling` + `/domain-modeling` for decision tickets; `/prototype`
  for UX-shape tickets; `/research` if a data-source question needs digging.
- **Preference**: start minimal. "Just a list of commits" is a hard constraint on
  the first cut — richer views are explicitly out of scope (below).

### Code context (established while charting)

- **hg history today**: `frontend/.../project/[project_code]/+page.ts` runs a
  non-awaited `projectChangesets` GraphQL query (already client-side lazy), rendered
  by `HgLogView.svelte`. Backend: `Project.GetChangesets` → `HgService.GetChangesets`
  → hgweb `log?style=json-lex`. **No pagination/limit** — pulls the whole changelog.
  `Changeset` fields: `node, rev, date[], desc, user, parents, branch, tags, phase`.
- **FwLite/CRDT history**: rich list only exists inside FwLite
  (`LcmCrdt/HistoryService.ProjectActivity`, `[JSInvokable]`, local SQLite). LexBox
  **server has no history endpoint** — only raw sync (`CrdtController`). Server does
  hold `CrdtCommits` (Postgres) = `ServerCommit{ Id, HybridDateTime, Metadata
  (AuthorId/AuthorName), ProjectId }`; change bodies are **opaque `ServerJsonChange`
  blobs** (no derivable message without deserializing `$type`s).
- **Relevance signal**: `Project.GetHasHarmonyCommits` → GraphQL
  `project.hasHarmonyCommits: Boolean!` already exists (true iff ≥1 server CRDT commit).

## Decisions so far

<!-- one line per resolved ticket: gist + link -->

- [Make Mercurial history load on demand](issues/01-hg-on-demand-ux.md) — collapsed
  section + "Show history" button gates the fetch; keep today's unbounded whole-log
  fetch for v1 (pagination stays in fog), reuse `HgLogView`.
- [Define the server API that lists FwLite/CRDT history](issues/02-fwlite-history-server-api.md)
  — new GraphQL field `Project.harmonyCommits(limit)` → `HarmonyCommit{id, dateTime,
  authorName}` from `CrdtCommits`, newest-N hard cap (no load-more), gated on
  `hasHarmonyCommits`. hg & FwLite are two separate fields, not merged server-side.
- [Decide how hg and FwLite history are presented together](issues/03-combined-history-layout.md)
  — two separate labeled sections (hg "History" keeps its commit graph; new
  "FieldWorks Lite history" flat list below), each with its own on-demand toggle;
  FwLite section shown only when `hasHarmonyCommits`. No merged timeline.

## Not yet specified

<!-- fog toward the destination; graduates into tickets as the frontier advances -->

_(empty — all remaining work is the spec ticket)_

## Out of scope

<!-- ruled beyond this destination; never graduates -->

- Rich per-commit views: diffs, drill-down, the client-side commit-graph / train-tracks
  rendering. First cut is "just a list."
- Changing FwLite's in-app `HistoryService` or its `[JSInvokable]` surface.
- Deriving human-readable change *messages* for CRDT commits by deserializing change
  blobs server-side — ticket 02 kept this out; author + timestamp only.
- Reaching **older FwLite history** past the newest-N hard cap (load-more / paging for
  `harmonyCommits`) — ticket 02 hard-caps v1; a later enhancement, not this destination.
