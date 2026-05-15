# Linq2Db v6 regressions and workarounds in LcmCrdt

Captured during the .NET 10 + Linq2Db 5.4 → 6.2.1 upgrade
(branch `chore/update-nuget-pkgs-dotnet-10`, PR
[#2264](https://github.com/sillsdev/languageforge-lexbox/pull/2264)).

This file documents the two issues we worked around, the trade-off the
current code makes, and the actions to take when upstream lands a fix.

---

## TL;DR

| Symptom | Status | Action when fixed upstream |
|---|---|---|
| 9 specific Gridify filter tests fail with `LinqToDBException : The LINQ expression could not be converted to SQL` | Worked around; tests remain red | Drop the `.ToList()` calls in `LcmCrdtKernel.cs:SenseSemanticDomainsExpression` / `EntryPublishInExpression`; revert return type to `IQueryable<T>`; restore `throw new NotImplementedException(...)` in both `Json.QueryInternal` bodies |
| `QueryPerformanceTesting` assertion `< 0.5 µs/entry` now sees ~0.7–0.8 µs/entry | Test margin too tight; not a code bug | After the workaround revert above, recheck. If still tight on the GHA Windows runner, raise the assertion margin (see history of `Tim H:` comments in `QueryEntryTests.cs:79`) |

---

## What changed in Linq2Db v6 that caused this

v6 rewrote the query parser. The
[migration wiki](https://github.com/linq2db/linq2db/wiki/Linq-To-DB-6)
makes one statement that explains both regressions:

> *"For final query projection, linq2db doesn't try to translate it to
> SQL anymore and sets its value on the client during materialization."*

Paired with a new "single-query preamble" strategy for eager loading
([`ExpressionBuilder.EagerLoad.cs`](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.EagerLoad.cs)),
this means that an `ExpressionMethodAttribute` registered on a property
now fires in two distinct places where v5 only fired it in one:

1. **Query translation** (where we *want* it) — `s.SemanticDomains`
   becomes `Json.Query(Sql.Property<...>(s, "SemanticDomains"))`, which
   `[Sql.TableFunction("json_each", argIndices: [0])]` then turns into a
   `json_each(jsonb_column)` table subquery.
2. **Materialization** (where v5 didn't apply it) — after the row is
   read and EF's value converter has deserialized the jsonb column to
   `IList<SemanticDomain>`, v6 *also* runs the substitution lambda
   client-side over the deserialized list and assigns the result back
   into the property. This is what causes the regressions.

`IsColumn` on `ExpressionMethodAttribute` is documented to control
exactly this behavior (`IsColumn=false` should mean "not used during
materialization"). Verified locally that v6 ignores it for our case.

## Why the two regressions appear

### Regression 1 — `LoadWith` materialization

`Sense.SemanticDomains : IList<SemanticDomain>` has the rewrite
registered. v6's preamble materializer evaluates the substitution
client-side and tries to assign the result back to the property. If
the substitution returns `IQueryable<SemanticDomain>` (which is the
natural type — `json_each` is a table), v6 throws
`InvalidCastException : Unable to cast EnumerableQuery<T> to IList<T>`
inside `LinqToDB.Internal.Linq.QueryRunner.Mapper.ReMapOnException`.

Originally this broke every code path that loaded a Sense — ~280 of 461
`LcmCrdt.Tests` failures came from this one issue.

### Regression 2 — translator can't see through `.ToList()`

The fix for regression 1 is to make the substitution return `IList<T>`,
which means tacking `.ToList()` on the end. That works for
materialization (`List<T> : IList<T>`). But then the *translator* sees
chains like

```csharp
e.Senses.Any(s => s.SemanticDomains.Any(sd => sd.Code.Contains("Fruit")))
```

expand to

```csharp
e.Senses.Any(s => json_each(s.SemanticDomains).Select(v => v.Value).ToList().Any(sd => sd.Code.Contains("Fruit")))
```

and gives up with `LinqToDBException : The LINQ expression could not be
converted to SQL`. The `.ToList()` between the table function and the
`Any(...)` defeats the table-form recognition.

The current code chooses to keep regression 1 silenced (so 99% of the
suite passes) and accept that the nine filter tests below stay red.

## The 9 still-failing filter tests

All in `LcmCrdt.Tests.MiniLcmTests.QueryEntryTests`:

- `CanFilterByPublicationId`
- `CanFilterByPublicationId_AndSearch`
- `CanFilterSemanticDomainCodeContains`
- `CanFilterToMissingSemanticDomains`
- `CanFilterToMissingSemanticDomainsWithEmptyArray`
- `CanFilterToMissingSemanticDomains_AndSearch`
- `CanFilterToMissingPublishIn`
- `CanFilterToMissingPublishInWithEmptyArray`
- `CanFilterToMissingPublishIn_AndSearch`

They throw at SQL-translation time. **They do not silently fall back to
client-side evaluation** — the query never runs at all. So this is not
a correctness or perf cliff in production traffic; it's a capability
loss for these specific filter shapes until upstream fixes it.

## The performance regression (`QueryPerformanceTesting`)

Two tests assert `< 0.5 µs/entry` after a warmup phase
(`QueryEntryTests.cs:79`). On this branch the per-entry cost is
~0.7–0.8 µs.

Cause: v6's client-side rewrite runs `Json.QueryInternal(list).Select(v
=> v.Value).ToList()` for every loaded `Sense` row, even when nothing
in the query consumes `SemanticDomains` as a table. For a 50K–100K
entry test this is an extra allocation + iteration per row.

**What would make this faster again:** the same upstream fix that
closes Regression 1. If the materializer stops invoking our
substitution after deserialization (i.e. honors `IsColumn=false` again),
the per-row overhead disappears and we revert to v5-level performance.
Until then the test margin is the better lever — the in-source history
in `QueryEntryTests.cs` already shows Tim cycling through 1.0 → 1.3 →
0.5 as the GHA runners changed. Bumping back to ~1.0 µs/entry would
match Kevin's original baseline plus headroom.

## What we tried

| Attempt | Eager load (`LoadWith`) | Gridify content filter | Gridify "missing"/null filter |
|---|---|---|---|
| A. Drop `ExpressionMethodAttribute`, write `Json.Query(...)` explicitly in `EntryFilterMapProvider` | ✓ works | ✗ Gridify can't parse `Json.Query(...)` — `InvalidOperationException` at `ParseMethodCallExpression` | ✗ same |
| B. Keep `ExpressionMethodAttribute`, expression returns `IQueryable<T>` (no `.ToList()`) | ✗ `InvalidCastException` on every load (~280 fails) | ✓ would translate | n/a (load already broken) |
| **C. Keep `ExpressionMethodAttribute`, expression returns `IList<T>` via `.ToList()`** *(current)* | ✓ | ✗ translator can't see through `.ToList()` | ✗ same |
| D. C + `IsColumn = false` explicitly | same as C | same as C | same as C — v6 ignores it |

Untried, in increasing invasiveness:
- Change `Sense.SemanticDomains` / `Entry.PublishIn` from `IList<T>` to
  `IQueryable<T>` at the model level. Removes the type mismatch but is
  a wide breaking change in `MiniLcm`.
- Drop the json column entirely and model these as real one-to-many EF
  associations. No more `[Sql.TableFunction]` rewriting at all. Schema
  + migration + sync-format break.
- Pin `linq2db` to 5.4.x. Avoids the regression at the cost of every
  fix in the v6 line; conflicts with the rest of the .NET 10 upgrade.

## Links

- Linq To DB 6 migration guide:
  <https://github.com/linq2db/linq2db/wiki/Linq-To-DB-6>
- Related (similar v6 `ExpressionMethod` regressions, all fixed — none
  match our pair of symptoms):
  - <https://github.com/linq2db/linq2db/issues/4613>
  - <https://github.com/linq2db/linq2db/issues/4977>
  - <https://github.com/linq2db/linq2db/issues/5040>
  - <https://github.com/linq2db/linq2db/issues/5254>
- Eager-load refactor in flight (targets 6.4.0) — might fix or change
  this:
  <https://github.com/linq2db/linq2db/pull/5450>
- The file the stack trace points into:
  <https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.EagerLoad.cs>

## Affected source

- `backend/FwLite/LcmCrdt/Json.cs` — `QueryInternal` bodies must return
  a working iterator (not throw) because v6 invokes them client-side.
- `backend/FwLite/LcmCrdt/LcmCrdtKernel.cs` — the two
  `Sense.SemanticDomains` / `Entry.PublishIn` `ExpressionMethodAttribute`
  registrations and their substitution expressions
  (`SenseSemanticDomainsExpression` / `EntryPublishInExpression`).
- `backend/FwLite/LcmCrdt.Tests/MiniLcmTests/QueryEntryTests.cs:79` —
  perf-test assertion margin (history of past adjustments in comments
  just above the line).
