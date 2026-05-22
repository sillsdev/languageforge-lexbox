# Linq2Db v6 — shadow-property workaround in LcmCrdt

Captured during the .NET 10 + Linq2Db 5.4 → 6.2.1 upgrade
(branch `wip/linq2db-v6-attempts`, PR
[#2264](https://github.com/sillsdev/languageforge-lexbox/pull/2264)).

This document covers:

1. The current working solution (**TL;DR** + **Files** sections).
2. Why v6 broke things (**Root cause**).
3. Everything we tried (**Attempt history**) — kept so the next person walking
   into this doesn't repeat the dead ends.
4. What's still worth contributing upstream as community-benefit fixes
   (**Upstream plan**) — lexbox no longer blocks on these.

---

## TL;DR

Two `IList<T>` jsonb columns — `Sense.SemanticDomains` and `Entry.PublishIn` —
need a `json_each(...)` rewrite for queries (`Any`, `SelectMany`, etc.) but
*not* at entity materialization. v5 honored that split via
`ExpressionMethodAttribute(IsColumn = false)`. v6 ignores `IsColumn` and fires
the substitution at materialization too, which either casts wrong
(`EnumerableQuery<T>` → `IList<T>`) or invokes a `Sql.TableFunction` body
client-side.

The fix here:

- Add **non-column shadow properties** `Sense.SemanticDomainRows` and
  `Entry.PublishInRows` in `MiniLcm/Models/`. They return the underlying list
  in client context (so reflection-based deep equality and bulk-copy paths
  don't trip) and have no column mapping.
- In `LcmCrdtKernel`, attach the `[ExpressionMethod]` rewrite to those shadow
  properties via `FluentMappingBuilder.IsExpression(..., isColumn: false)`.
  `IsExpression` also calls `IsNotColumn()`, so BulkCopy and insert paths
  don't read them.
- Route Gridify filter projections through the shadow properties
  (`EntryFilterMapProvider.EntryPublishInId`,
  `EntrySensesSemanticDomainsCode`).
- The materialization expression doesn't reference the shadow properties, so
  v6's `ExposeExpressionVisitor` never sees them — the substitution fires
  only when LINQ translation hits `e.PublishInRows` in a query.

Result: all of `LcmCrdt.Tests` passes, including the perf assertion and the
nine filter shapes that the earlier `.ToList()` workaround left red.

---

## Root cause — what changed in v6

v6 rewrote the query parser. The
[migration wiki](https://github.com/linq2db/linq2db/wiki/Linq-To-DB-6)
makes one statement that explains both regressions:

> *"For final query projection, linq2db doesn't try to translate it to SQL
> anymore and sets its value on the client during materialization."*

Paired with a new "single-query preamble" strategy for eager loading
([`ExpressionBuilder.EagerLoad.cs`](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.EagerLoad.cs)),
this means an `ExpressionMethodAttribute` registered on a property now fires
in two places where v5 only fired it in one:

1. **Query translation** (where we *want* it) — `s.SemanticDomains` becomes
   `Json.Query(Sql.Property<...>(s, "SemanticDomains"))`, which
   `[Sql.TableFunction("json_each", argIndices: [0])]` then turns into a
   `json_each(jsonb_column)` table subquery.
2. **Materialization** (where v5 didn't apply it) — after the row is read and
   EF's value converter has deserialized the jsonb column to
   `IList<SemanticDomain>`, v6 *also* runs the substitution lambda
   client-side over the deserialized list and assigns the result back into
   the property. This is what causes the regressions.

`IsColumn` on `ExpressionMethodAttribute` is documented to control exactly
this behavior (`IsColumn=false` should mean "not used during
materialization"). Verified locally that v6 ignores it: `TableBuilder.TableContext.MakeExpression`
now passes `fullEntity` through `Builder.ConvertExpressionTree`, which routes
through `ExposeExpressionVisitor` and expands every `[ExpressionMethod]`
regardless of `IsColumn`.

### The two original regressions

**Regression 1 — `LoadWith` materialization.** With the rewrite on the
property and returning `IQueryable<T>` (natural shape — `json_each` is a
table), v6's materializer evaluates the substitution client-side and throws
`InvalidCastException : Unable to cast EnumerableQuery<T> to IList<T>` inside
`LinqToDB.Internal.Linq.QueryRunner.Mapper.ReMapOnException`. Originally
broke ~280 of 461 tests.

**Regression 2 — translator can't see through `.ToList()`.** Forcing the
substitution to return `IList<T>` via trailing `.ToList()` fixes
materialization but defeats SQL translation: filter chains like
`e.Senses.Any(s => s.SemanticDomains.Any(sd => sd.Code.Contains("Fruit")))`
expand to
`...json_each(s.SemanticDomains).Select(v => v.Value).ToList().Any(sd => ...)`,
and v6 gives up with `LinqToDBException : The LINQ expression could not be
converted to SQL`.

---

## Attempt history

| # | Approach | Eager load (`LoadWith`) | Gridify content filter | Gridify "missing"/null filter |
|---|---|---|---|---|
| A | Drop `[ExpressionMethod]`, write `Json.Query(...)` explicitly in `EntryFilterMapProvider` | ✓ works | ✗ Gridify can't parse `Json.Query(...)` — `InvalidOperationException` at `ParseMethodCallExpression` | ✗ same |
| B | Keep `[ExpressionMethod]`, expression returns `IQueryable<T>` (no `.ToList()`) | ✗ `InvalidCastException` on every load (~280 fails) | ✓ would translate | n/a (load already broken) |
| C | Keep `[ExpressionMethod]`, expression returns `IList<T>` via `.ToList()` | ✓ | ✗ translator can't see through `.ToList()` | ✗ same |
| D | C + `IsColumn = false` explicitly | same as C | same as C | same as C — v6 ignores it |
| E1 | Shadow **extension method** `e.PublishInAsRows()` with `[ExpressionMethod]` | ✓ | ✗ Gridify's `ParseMethodCallExpression` only handles `MemberExpression` / `Select` / `SelectMany` / `Where` as chain root; a method-call root throws | ✓ (combined with converter change — see below) |
| **F** | **Shadow *property*** `e.PublishInRows` with `IsExpression(..., isColumn:false)` *(current)* | ✓ | ✓ | ✓ |

### Why the shadow approach works as a property but not a method

Gridify's `LinqQueryBuilder.ParseMethodCallExpression` switches on the first
argument of the `Select(...)` projection. Only four shapes match:
`MemberExpression`, or a nested `Select` / `SelectMany` / `Where`
`MethodCallExpression`. A user-defined extension call like
`e.PublishInAsRows()` matches none and throws `InvalidOperationException`. A
property access (`e.PublishInRows`) is a `MemberExpression`, so it does
match.

### Why v6's materializer doesn't trip on the shadow

The shadow property is unmapped (`[NotMapped, JsonIgnore]`, plus
`IsNotColumn()` via `IsExpression`). v6's materialization expression is
shaped like `new Entry { Col1 = ..., Col2 = ... }` over the mapped columns
only. `ExposeExpressionVisitor` only expands `[ExpressionMethod]` when it
actually walks past that member in some expression tree. The shadow never
appears in the materialization tree, so its substitution never fires there.

### Other untried alternatives (still escalation paths if F ever breaks)

In increasing invasiveness:

- Change `Sense.SemanticDomains` / `Entry.PublishIn` from `IList<T>` to
  `IQueryable<T>` at the model level. Removes the type mismatch but is a wide
  breaking change in `MiniLcm`.
- Drop the json column entirely and model these as real one-to-many EF
  associations. No more `[Sql.TableFunction]` rewriting at all. Schema +
  migration + sync-format break.
- Pin `linq2db` to 5.4.x. Avoids the regression at the cost of every fix in
  the v6 line; conflicts with the rest of the .NET 10 upgrade.

---

## Filter-converter changes

Without the property-level rewrite, `e.PublishIn == null` no longer maps to
"NOT EXISTS json_each(...)" — it lowers to plain column `IS NULL`, which
matches nothing because empty lists are stored as `"[]"`, never SQL NULL.
The two affected converters
(`EntryPublishInConverter`, `EntrySensesSemanticDomainsConverter`) therefore
use `NormalizeEmptyToEmptyList` instead of `NormalizeEmptyToNull`, generating
`column = '[]'` — the same pattern `EntryComplexFormTypesConverter` already
used.

---

## Two related v6 fixes that landed in `Json.cs`

| Concern | Where |
|---|---|
| Peel `Sql.Alias(...)` wrap from `IExtensionCallBuilder` lambda arg bodies | `JsonValuePathBuilder.BuildParameterPath` |
| `PseudoFunctions.MakeTryConvert` was dropped in v6 — build the `SqlFunction` directly | `JsonValuePathBuilder.Build` |

The Alias-peel exists because `ExposeExpressionVisitor` wraps every
`[ExpressionMethod]` substitution in `Sql.Alias(real_expr, "<member>")` as a
column-alias hint, and that wrap leaks into user-written
`IExtensionCallBuilder` arg lambdas. `Json.Value(p, p => p.Id.ToString())` is
what trips it in our code; the same shape would affect any other linq2db
user with `[ExpressionMethod]` + custom extension builders.

---

## Upstream plan (community-benefit, not lexbox-blocking)

With the shadow-property approach in place, lexbox no longer depends on any
linq2db upstream fix. The two items below are still worth filing as
community contributions — other users will hit them — but they're off our
critical path.

### PR A — Honor `IsColumn=false` at entity materialization

The clean win. Restores the documented `[ExpressionMethod].IsColumn`
contract.

- **Doc contract:** `IsColumn`'s XML doc reads: *"When applied to property
  and set to true, Linq To DB will load data into property using expression
  during entity materialization."* The default (`false`) should opt out of
  materialization-time invocation.
- **Where it broke:** `TableBuilder.TableContext.MakeExpression` now passes
  `fullEntity` through `Builder.ConvertExpressionTree`, which routes through
  `ExposeExpressionVisitor` and expands every `[ExpressionMethod]`
  regardless of `IsColumn`.
- **Proposed fix:** thread a `calculatedColumnsOnly` flag through one
  materialization call site. `ExposeExpressionVisitor.ConvertExpressionMethodAttribute`
  returns `null` when `_calculatedColumnsOnly && !attr.IsColumn`. Caller
  falls back to bare member access.
- **Sample repro:** a `Foo` with `IList<Bar> Bars` carrying a `JsonEach`-style
  substitution and `IsColumn = false` — `db.GetTable<Foo>().ToList()` throws
  in v6 but works in v5.
- **Risk profile:** low. Anyone relying on v6's regression behavior would
  have been broken on v5 too; they should set `IsColumn=true`.

### PR C — Peel `Sql.Alias` from `IExtensionCallBuilder` lambda args

Narrow, surface-area-only fix. No public contract change.

- **Symptom:** `ExposeExpressionVisitor` wraps every `[ExpressionMethod]`
  substitution in `Sql.Alias(real_expr, "<member-name>")`. The wrap is
  invisible to the SQL builder but observable inside user-written
  `Sql.IExtensionCallBuilder` lambda-arg walkers as `Alias(real_expr, "ToString")`.
- **Proposed fix:** in `Sql.ExtensionAttribute.GetExtensionParam`, peel
  `Sql.Alias(...)` from lambda-argument bodies before invoking the user
  builder. Top-level (non-lambda) argument expressions are left alone.
- **Lexbox-side workaround already in `Json.cs`** — see the table above.
- **Risk profile:** low. The wrap was never part of the documented
  extension-builder contract.

### PR B — retired

Previously envisioned as "suppress collection-typed `[ExpressionMethod]`
substitution inside `Equal`/`NotEqual` against null" so that
`e.PublishIn == null` would lower to column `IS NULL`. The shadow-property
approach makes this moot — the bare null check now lands on the column
naturally, no engine change needed. Don't open this PR.

### Sequencing

A → C, parallelizable. Cadence on `linq2db/linq2db` (sampled 2026-05-18):
~10 PRs/week merged, **6.3.0 shipped 2026-05-17**, **6.4.0 version bump
merged 2026-05-18**. Small bug-fixes with a linked issue land same-day to
3-day; medium fixes 1–2 weeks. Issue-first is convention; PR titles read
`Fix #NNNN: <title>`. Maintainers worth tagging: `MaceWindu` (release
management + cross-provider review), `igor-tkachev` (original author),
`sdanyliv` (most active feature contributor).

If we file A + C, realistic landing window is 1–2 weeks each, with a shot
at the 6.4.x line.

---

## Files

- `MiniLcm/Models/Entry.cs`, `MiniLcm/Models/Sense.cs` — shadow properties.
- `LcmCrdt/LcmCrdtKernel.cs` — `IsExpression` registration and the rewrite
  factories (`SenseSemanticDomainRowsExpression`,
  `EntryPublishInRowsExpression`).
- `LcmCrdt/Json.cs` — `Sql.Alias` peel; `TRY_CONVERT` direct construction.
- `LcmCrdt/EntryFilterMapProvider.cs` — projections routed through shadow
  properties; converters use `NormalizeEmptyToEmptyList`.
- `LcmCrdt.Tests/MiniLcmTests/QueryEntryTests.cs:79` — perf-test assertion
  margin (history of past adjustments in comments above the line).

---

## Trade-offs

- The shadow accessor is a soft lie in client context — anyone calling
  `entry.PublishInRows` from non-query code gets the underlying list, not a
  `json_each` table. Surface area is tiny (two properties, marked
  `[NotMapped, JsonIgnore]`) and the comment on each points back here.
- The shape leaks a query-engine concern into `MiniLcm`'s shared domain
  model. Acceptable cost given the alternative (waiting on PR A or shipping
  the `.ToList()` workaround with 9 red tests).
- If PR A lands and we upgrade to a fixed linq2db release, the shadow
  properties can be collapsed back into bare `[ExpressionMethod]` on the
  underlying columns. Not load-bearing for lexbox.

---

## Links

- Linq To DB 6 migration guide:
  <https://github.com/linq2db/linq2db/wiki/Linq-To-DB-6>
- Eager-load refactor in flight (targets 6.4.0) — may fix or change this:
  <https://github.com/linq2db/linq2db/pull/5450>
- `ExpressionBuilder.EagerLoad.cs` (the file the stack trace points into):
  <https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.EagerLoad.cs>
- Related (similar v6 `[ExpressionMethod]` regressions, all fixed — none
  match our pair of symptoms but useful as precedent):
  - <https://github.com/linq2db/linq2db/issues/4613>
  - <https://github.com/linq2db/linq2db/issues/4977>
  - <https://github.com/linq2db/linq2db/issues/5040>
  - <https://github.com/linq2db/linq2db/issues/5254>
- Local upstream-side scratch (PR drafts, repro shapes for A and C):
  `D:\code\linq2db\UPSTREAM-PLAN.md` (outside this repo).

---

## Cctor patcher (Android-only)

Separate from the shadow-property workaround above. `EFCoreMetadataReader+SqlTransparentExpression`'s `.cctor` in `linq2db.EntityFrameworkCore` 10.3.x looks up a `GetConstructor((ExceptExpression, RelationalTypeMapping))` signature that doesn't exist on the type (the only declared ctor takes `(ConstantExpression, RelationalTypeMapping?)`) — `GetConstructor` returns null, `_ctor = ...` throws `InvalidOperationException`, the cctor fails, and any later access to the type explodes with `TypeInitializationException`.

Desktop is silent: the class is `beforefieldinit`, only `Quote()` reads the affected static fields, and our CRDT workload never calls `Quote()`. Mono/Android AOT eagerly initializes the type on the first CRDT save and detonates there.

**Where the patcher lives:** `backend/FwLite/FwLiteMaui/build/Linq2DbCctorPatcher/` — a `net10.0` Mono.Cecil console tool. MSBuild targets in `FwLiteMaui.csproj` build it and run it against every staged copy of `linq2db.EntityFrameworkCore.dll` under `$(IntermediateOutputPath)`. The patcher rewrites the cctor body to `ret` and replaces `Quote()` with `throw new NotImplementedException()` so any surprise caller fails loudly instead of NRE'ing on the now-null `_ctor` field. Patched dlls get a sibling sentinel file (`<dll>.cctor-patched`) and the MSBuild target is `Inputs`/`Outputs`-incremental.

**Why it's only here, not in `backend/build/`:** only `FwLiteMaui` targets `net10.0-android`. If `FwHeadless` or `FwLiteWeb` ever start targeting Android and reference `linq2db.EntityFrameworkCore`, lift the patcher into a shared tools dir and reference it from each consumer.

**Kill-switch (delete the patcher entirely when):**

1. `_VerifyLinq2DbEfCoreVersionPin` in `FwLiteMaui.csproj` fails because the version moved outside `10.3.x` / `10.4.x`.
2. Manually verify the bug — `RuntimeHelpers.RunClassConstructor` on `LinqToDB.EntityFrameworkCore.EFCoreMetadataReader+SqlTransparentExpression` from a net10.0 test process loading the new dll.
3. If the cctor no longer throws → delete `backend/FwLite/FwLiteMaui/build/Linq2DbCctorPatcher/` and the four targets in `FwLiteMaui.csproj` (`_VerifyLinq2DbEfCoreVersionPin`, `_BuildLinq2DbCctorPatcher`, `_CollectLinq2DbStagedAssemblies`, `_PatchLinq2DbSqlTransparentExpressionCctor`).
4. If it still throws → widen the version pin regex in `_VerifyLinq2DbEfCoreVersionPin`.

**Upstream fix:** <https://github.com/linq2db/linq2db/pull/5546> (open, targets linq2db 6.4.0 — not yet released).
