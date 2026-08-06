# Spec: On-demand project-page history (hg + FieldWorks Lite)

Implementation-ready spec for the "On-demand history on the LexBox project page" map.
Every design decision is settled in the linked tickets; this doc folds them into a
build plan for `/implement`. Hand-off deliverable — no code written here.

## Goal

On the LexBox project page, stop auto-loading Mercurial history on every visit (it's
slow), and add a FieldWorks Lite (CRDT) history list when the project has one. First
cut is a bare list of commits: author name + timestamp.

## Decisions this spec implements

- [hg history on demand](issues/01-hg-on-demand-ux.md) — collapsed section + "Show
  history" button gates the fetch; unbounded whole-log fetch kept for v1.
- [FwLite history server API](issues/02-fwlite-history-server-api.md) — new GraphQL
  field `Project.harmonyCommits(limit)` → `HarmonyCommit{id,dateTime,authorName}`,
  newest-N hard cap, gated on `hasHarmonyCommits`.
- [Combined history layout](issues/03-combined-history-layout.md) — two separate
  labeled sections, each with its own on-demand toggle; no merged timeline.

## Pre-flight — components touched

Backend: `LexCore.Entities` (new DTO + resolver on `Project`) → `LexData`
(`CrdtCommits` query) → HotChocolate schema → `frontend/schema.graphql` (regen).
Frontend: `+page.ts` (queries/stores) → `+page.svelte` (two collapsible sections +
triggers) → new `HarmonyLogView.svelte` → generated `$lib/gql/types` (regen) → i18n
(`en.json` / message catalog). No FwHeadless, no FwLite client, no Harmony substrate.

---

## Backend

### 1. `HarmonyCommit` DTO
New record beside `Changeset` in `backend/LexCore/Entities/Project.cs`:
```csharp
public record HarmonyCommit(Guid Id, DateTimeOffset DateTime, string? AuthorName);
```

### 2. Resolver on `Project`
Add a method mirroring `GetChangesets` / `GetHasHarmonyCommits`
(`backend/LexCore/Entities/Project.cs`):
```csharp
public async Task<HarmonyCommit[]> GetHarmonyCommits(
    IDbContextFactory<LexBoxDbContext> dbContextFactory, int limit = 50)
{
    if (Type is not (ProjectType.Unknown or ProjectType.FLEx)) return [];
    await using var db = await dbContextFactory.CreateDbContextAsync();
    var commits = await db.CrdtCommits(Id)
        .OrderByDescending(c => c.HybridDateTime.DateTime)
        .ThenByDescending(c => c.HybridDateTime.Counter)
        .Take(limit)
        .ToArrayAsync();                      // materialize BEFORE reading Metadata
    return [.. commits.Select(c =>
        new HarmonyCommit(c.Id, c.HybridDateTime.DateTime, c.Metadata.AuthorName))];
}
```

**Server-side mapping caveats (verified in `CommitEntityConfiguration.cs`):**
- `HybridDateTime` is a mapped complex property → `.DateTime` / `.Counter` are real
  columns, so `OrderByDescending` + `Take` translate to SQL. Order + cap happen in the
  DB.
- `Metadata` is stored via `.HasConversion` as a **serialized string column** (NOT
  queryable jsonb, unlike the FwLite `LcmCrdtDbContext` side). So `c.Metadata.AuthorName`
  must be read **after** materialization (hence `ToArrayAsync()` first). Do not
  `.Select(... c.Metadata.AuthorName)` inside the `IQueryable` — it won't translate.
- Confirm the exact `HybridDateTime` member names against the pinned
  `SIL.Harmony.Core` package (`DateTime`/`Counter` per current usage) before coding.
- `limit` default 50; keep the arg optional. Consider clamping (e.g. max a few
  hundred) to avoid abuse.

### 3. DataLoader vs factory
`hasHarmonyCommits` uses a batch dataloader because it fans across many projects in a
list. `harmonyCommits` is only ever requested for a single project page, so a plain
`IDbContextFactory` query is fine — no batching needed. (If a future list view needs
per-row commit lists, revisit.)

### 4. GraphQL schema
`harmonyCommits` is auto-exposed by HotChocolate from the resolver method (like
`changesets`). Add a `@cost` weight consistent with siblings (`changesets` is
`weight: "10"`). Regenerate `frontend/schema.graphql` via the running backend
(`DevGqlSchemaWriterService` writes it on dev startup) and commit the diff — expect a
new `harmonyCommits(limit: Int! = 50): [HarmonyCommit!]!` field and a `HarmonyCommit`
type. No `[Authorize]` beyond what `Project` already enforces (same visibility as
`changesets`).

---

## Frontend

### 5. hg history — gate the existing fetch (ticket 01)
In `+page.ts`, the `projectChangesets` `queryStore` currently fires client-side on
load (the store is already built "pausable" — see the comment in `load()`).
- Initialize that query **paused** so it does not fetch on page load.
- Resume it when the user clicks "Show history". Keep the derived
  `{fetching, changesets}` shape and `HgLogView` reuse unchanged.
- Keep `_refreshProjectRepoInfo` working (it re-queries `...Changesets`); after a
  refresh it should only matter if the section is already open.

### 6. FwLite history — new query (ticket 02)
Add a fragment + query in `+page.ts`, fetched on demand and **only when**
`hasHarmonyCommits` (already selected in `projectPage`):
```graphql
fragment HarmonyCommits on Project {
  harmonyCommits { id dateTime authorName }
}
query projectHarmonyCommits($projectCode: String!) {
  projectByCode(code: $projectCode) { id code ...HarmonyCommits }
}
```
Model it as a second pausable store, resumed by its own "Show FieldWorks Lite history"
button — independent of the hg store (ticket 03).

### 7. UI — two collapsible sections (ticket 03)
In `+page.svelte`, the current single `project_page.history` block (~lines 618-635)
becomes two stacked, independently-collapsible sections:
1. **History** (hg) — existing `HgLogView`; the section title row keeps the "Open in
   hgweb" link and gains a "Show history" toggle that reveals the view + resumes the
   hg store. Collapsed by default.
2. **FieldWorks Lite history** — rendered `{#if project.hasHarmonyCommits}` only. A
   "Show FieldWorks Lite history" toggle reveals a new `HarmonyLogView` + resumes the
   harmony store. Collapsed by default; absent entirely when `hasHarmonyCommits` is
   false (no empty state).

Use whatever collapse primitive the app already uses (DaisyUI `collapse`/`btn`);
match the existing loading-spinner pattern (`Loader` + a `*.loading` string) for each
section while its store is `fetching`.

### 8. New component `HarmonyLogView.svelte`
Flat table/list (no commit graph): columns **Date** (`$date(dateTime)`) and **Author**
(`authorName`). Author fallback when null: show the raw author id if surfaced, else a
localized "Unknown" string. Loading + empty states mirror `HgLogView`. Keep it dumb —
props `logEntries`, `loading`.

### 9. i18n
New message keys (add to `en` catalog, follow existing `project_page.hg.*`):
- `project_page.history_show` / a "Show history" label (or reuse an existing collapse
  label).
- `project_page.harmony.title` = "FieldWorks Lite history"
- `project_page.harmony.show` = "Show FieldWorks Lite history"
- `project_page.harmony.loading`, `project_page.harmony.empty`,
  `project_page.harmony.unknown_author`
Run the viewer i18n extraction if applicable; the main frontend uses its own catalog —
match the file the other `project_page.*` keys live in.

### 10. Codegen
After the schema regen (step 4), run the frontend GraphQL codegen so `$lib/gql/types`
gains `HarmonyCommit`, `ProjectHarmonyCommitsQuery`, and the `HarmonyCommits` fragment.
Commit the generated diff.

---

## Presence / empty-case matrix

| Project | hg section | FwLite section |
|---|---|---|
| Has repo, `hasHarmonyCommits` false | shown, collapsed | **absent** |
| Has repo, `hasHarmonyCommits` true | shown, collapsed | shown, collapsed |
| Non-FLEx (no harmony) | shown as today | absent |

No project shows a merged view.

## Testing

- **Backend**: unit-test `GetHarmonyCommits` ordering (newest-first), the `limit` cap,
  the non-FLEx-type short-circuit returning `[]`, and null `AuthorName` passthrough.
  A `RequiresDb` integration test can seed `CrdtCommits` rows and assert the projected
  DTO. Do NOT run the full lexbox-stack suites locally (per AGENTS.md) — rely on CI for
  those.
- **Frontend**: the page renders without firing either history query on load (assert
  no fetch until the toggle); clicking each toggle fetches and renders its list;
  FwLite section absent when `hasHarmonyCommits` false. Playwright for the main
  `frontend/` suite is CI-only per AGENTS.md.
- Schema + gql codegen diffs are committed and green.

## Out of scope (from the map)

Diffs / drill-down / commit-graph for FwLite; deriving CRDT change *messages*;
load-more / paging past the FwLite hard cap; merged timeline + author-identity
reconciliation; hg pagination.

## Follow-ups worth filing later

- Reaching older FwLite history (paging `harmonyCommits`).
- Optional hg bounding if the unbounded on-click fetch is still too slow for large
  projects.
