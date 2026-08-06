# Define the server API that lists FwLite/CRDT history

Type: grilling
Status: resolved

## Question

What should a new LexBox-server query for FwLite history return, and how is it shaped?

The LexBox project page can't call FwLite's in-app `HistoryService` (that runs inside
FwLite against local SQLite). The server only holds `CrdtCommits` (Postgres):
`ServerCommit{ Id (Guid), HybridDateTime (DateTime+Counter), Metadata (AuthorId,
AuthorName), ProjectId }`. Change bodies are **opaque `ServerJsonChange` blobs**.

Decide:
- **Fields per entry**: minimal = `{ id, timestamp, authorName }` (matches "just a
  list, user + timestamp"). AuthorId is a GUID; AuthorName may be null — what's the
  display fallback?
- **Message/summary**: none server-side without deserializing change `$type`s. Ship
  author+timestamp only for v1 (deriving a summary is currently Out of scope — reopen
  it here only if the list looks too bare)?
- **Transport**: new field on GraphQL `Project` (next to `changesets` /
  `hasHarmonyCommits`) vs a new REST endpoint. Gate on `hasHarmonyCommits`.
- **Ordering + bounding**: sort by `HybridDateTime`; paged or capped like ticket 01?

Context: `LexBoxDbContext.CrdtCommits(projectId)`,
`CommitEntityConfiguration.cs`, `IsHarmonyProjectDataLoader.cs`,
`Project.GetHasHarmonyCommits` (`schema.graphql:433`), `CrdtController` (sync-only).

Output: the decided entry shape + transport for FwLite history, ready to spec.

## Answer

**Transport: a new GraphQL field on `Project`**, mirroring the existing `changesets`
and `hasHarmonyCommits` resolvers (methods on the `Project` entity that HotChocolate
auto-exposes). Proposed `harmonyCommits(limit: Int): [HarmonyCommit!]!` (name matches
`hasHarmonyCommits`). No REST. The UI gates the fetch on `hasHarmonyCommits` the same
way the "Show history" button gates hg (ticket 01).

**Row fields: author name + timestamp only.** New DTO, e.g.
`HarmonyCommit { id: ID!, dateTime: DateTime!, authorName: String }`:
- `id` — `ServerCommit.Id` (Guid), the stable row key; not necessarily displayed.
- `dateTime` — from `ServerCommit.HybridDateTime.DateTime` (`DateTimeOffset`).
- `authorName` — from `ServerCommit.Metadata.AuthorName` (`CommitMetadata`, JSON
  column). May be null → UI fallback to author id or "Unknown". No commit *message*/
  summary (change bodies are opaque `ServerJsonChange` blobs — deriving one stays
  Out of scope).

**Bounding: hard cap to newest N, no load-more (v1).** Query
`LexBoxDbContext.CrdtCommits(Id)` ordered by `HybridDateTime` **descending**, `.Take(N)`
(default e.g. 50; optional `limit` arg, no cursor/offset). Chosen over hg's
unbounded-defer because CRDT volume is higher. Consequence: older FwLite history is
**not reachable from the page in v1** — pagination/load-more is deferred (see map
Out of scope).

Resolver shape (follows `GetChangesets`/`GetHasHarmonyCommits`):
```csharp
public async Task<HarmonyCommit[]> GetHarmonyCommits(IDbContextFactory<LexBoxDbContext> f, int limit = 50)
    // query CrdtCommits(Id).OrderByDescending(c => c.HybridDateTime).Take(limit)
    //   .Select(c => new HarmonyCommit(c.Id, c.HybridDateTime.DateTime, c.Metadata.AuthorName))
```
(Exact DI/dataloader vs factory left to the spec ticket; a per-project single-shot
query likely doesn't need a batch dataloader the way `hasHarmonyCommits` does.)

Implications:
- Resolves the map's "unified vs parallel data path" fog: hg and FwLite are **two
  separate GraphQL fields** on `Project`, not one merged query. Whether the *UI*
  merges them is the layout ticket.
- Author-identity reconciliation (hg `user` string vs CRDT author) only bites if the
  layout ticket picks a merged timeline.
