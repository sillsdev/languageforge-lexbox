# .NET style — review checklist

Read this in full before filing findings on any `**/*.cs` diff.

Authoritative AGENTS.md: `backend/AGENTS.md` and `backend/LexBoxApi/AGENTS.md`.
This is the review pass — concrete patterns and grep targets.

## Async

- **No** `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` in
  async-reachable code paths. Causes thread-pool starvation, deadlocks in
  some sync contexts. Replace with `await`.
- All async methods return `Task` / `Task<T>` / `ValueTask` / `ValueTask<T>`.
  No `async void` except event handlers.
- `CancellationToken` is the **last** parameter. Pass it through, don't drop
  it.
- Don't `await` then `.ConfigureAwait(false)` in ASP.NET / library code
  unless you've thought about it — the modern guidance is mixed and the
  codebase doesn't use it consistently.

Grep:
- `\.Result\b` → flag every match in `**/*.cs`.
- `\.Wait\(\)` → flag.
- `GetAwaiter\(\)\.GetResult\(\)` → flag.
- `async void` → flag unless event handler.

## Nullable reference types

- Globally enabled, warnings are errors.
- No smuggled `!` to silence null warnings without justification.
- No `default!` without a comment explaining why null is impossible.
- `required` for mandatory init-only properties.
- `?` for optional.
- Records: nullable annotations on positional parameters work — use them.

## Records vs classes

- **Records** for DTOs, value objects, immutable data
  (e.g. `WritingSystems`, `ProjectSnapshot`, `DryRunSyncResult`).
- **Classes** for mutable domain entities (e.g. `Entry`, `Sense`).
- Both can implement `IObjectWithId<T>` for CRDT entities.
- Records with one-line bodies: prefer positional syntax.
- Records with complex behavior: brace syntax with explicit properties.

## Primary constructors

```csharp
public class Service(ILogger<Service> logger, Dependency dep)
{
    public async Task DoThing()
    {
        logger.LogInformation("…");
        await dep.Whatever();
    }
}
```

Use for one-line DI. Don't over-use — if the constructor body had real
logic, keep the explicit ctor.

## Logging

- Constructor-injected `ILogger<T>`.
- Structured logging: `logger.LogInformation("Imported {Type} {Id}", "entry", entry.Id)`.
- Never `Console.WriteLine` in production code (only in tests/local
  benchmarks).
- For human-readable durations: `timeSpent.Humanize(2)`.

Grep:
- `Console\.Write` → flag in `backend/**/*.cs` (excluding test/benchmark
  projects).
- String-interpolation in log messages (`$"…"`) → soft flag; structured
  template is preferred.

## DI lifetimes

- `AddScoped` for per-request services, including `IMiniLcmApi`,
  `CrdtMiniLcmApi`, database contexts.
- `AddSingleton` for app-wide services like `CrdtProjectsService`.
- `AddTransient` for one-shot helpers.

Services registered in `*Kernel.cs` files. If a new service is added in the
diff, it should be registered in the appropriate kernel.

## Exceptions

- Custom exception types for known failure modes
  (`SyncObjectException`, `DuplicateObjectException`, `NotFoundException`).
- Wrap external exceptions with context:
  `throw new SyncObjectException($"Failed to sync {entry}", e);`
- Don't catch and swallow without re-throwing, logging, or returning a
  fail result. "Silent" catches are a frequent finding.

## File-scoped namespaces

```csharp
namespace MiniLcm.Models;

public record Entry { … }
```

Not braced (`namespace MiniLcm.Models { … }`). Editor config enforces this.

## Tests

- xUnit (`[Fact]`, `[Theory]`, `[InlineData]`).
- FluentAssertions for assertions: `.Should().NotBeNull()`,
  `.Should().BeEquivalentTo()`.
- **Prefer `BeEquivalentTo` over `BeSubsetOf`** when sets really should be
  equal. `BeSubsetOf` lets bugs slip (PR #2219).
- **Enum data:** use `Enum.GetValues<T>()` instead of casting `int`s.
- New enum values (e.g. `RegressionVersion`) need parallel `[InlineData]`
  rows.
- Async tests: `IAsyncLifetime` for setup/teardown.
- Test class structure: base class in `MiniLcm.Tests/` with implementations
  in both `LcmCrdt.Tests/` and `FwDataMiniLcmBridge.Tests/` where dual-impl
  coverage is needed.
- Tests should clean up after themselves on failure — unique filenames
  derived from a `code` variable (PR #2219).
- **No meaningless assertions:** `expect(true).toBe(true)`,
  `.Should().BeTrue()` on a literal, `assert.IsNotNull(null)` — all
  blocking findings. Root `AGENTS.md` § VIGILANCE.
- **Reproduce sync bugs with a test** using `DryRunMiniLcmApi` before
  fixing — review thread on PR #2252 cites this.

Grep:
- `BeSubsetOf` → check whether `BeEquivalentTo` would catch more bugs.
- `Should\(\)\.BeTrue\(\)` followed by a constant → meaningless assertion.
- `\[Skip\]` / `\[SkipWhen\]` → ask: justified?

## EF Core / migrations

See `references/fwlite-sync-checklist.md` § 7 for the `ON CONFLICT IGNORE`
gotcha. Other notes:

- `IQueryable` projections (`[UseProjection]`, `[UseFiltering]`,
  `[UseSorting]`) in GraphQL.
- Don't bypass with `.ToListAsync()` then in-memory filter unless small
  fixed cardinality.
- Mutations return the modified entity, not `void`.
- Auth checks (`[Authorize]`, `[AdminRequired]`, `[ProjectMember]`) at the
  appropriate layer — controller/GraphQL, not sync code path (PR #2215).

## EditorConfig

`backend/.editorconfig`:
- 4-space indent
- Style warnings → errors

Run `dotnet format LexBoxOnly.slnf --verify-no-changes` (or
`FwLiteOnly.slnf` if FwLite-touched) to catch drift. If it returns
non-zero, run `dotnet format` (without `--verify-no-changes`) to fix.

## Findings tone

- `.Result` / `.Wait()` → **important** (functional bug waiting to
  happen). **Blocking** in async-heavy paths.
- Missing `CancellationToken` propagation → **important**.
- Smuggled `!` for null silencing → **important**, ask for justification.
- Misnamed parameter (e.g. `file` when the value is a regex) → **nit**
  unless it's a public API.
- `Console.WriteLine` debug print → **blocking** (auto-removable in Pass A).
- Test with meaningless assertion → **blocking**.
- `BeSubsetOf` where set equality intended → **important**.
