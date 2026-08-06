# Define the server API that lists FwLite/CRDT history

Type: grilling
Status: open

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
