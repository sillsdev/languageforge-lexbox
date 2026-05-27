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

## Records, classes, primary constructors

- **Records** for DTOs and immutable data; **classes** for mutable
  domain entities. Positional syntax for one-line records.
- Primary constructors for one-line DI: `public class Service(ILogger<Service> logger)`.
  Keep an explicit constructor when the body has real logic.

## Logging

- Inject `ILogger<T>`; use structured templates
  (`logger.LogInformation("Imported {Type} {Id}", "entry", entry.Id)`).
- No `Console.WriteLine` in production. Grep `Console\.Write` in
  `backend/**/*.cs` excluding test/benchmark projects.

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

## Resource disposal & lifetime

Async code that can throw mid-operation needs `using` (or `await using`)
around any disposable it owns. The team has been bitten by leaked file
handles and streams when `CopyToAsync` (or similar) threw before disposal
ran.

```csharp
// good
await using var stream = file.OpenReadStream();
await response.CopyToAsync(stream);

// bad — if CopyToAsync throws, stream isn't disposed
var stream = file.OpenReadStream();
await response.CopyToAsync(stream);
stream.Dispose();
```

The disposal must run on the unwind path. `await using` for async-
disposable, `using` for sync. `finally` block when the disposal is
conditional.

## Collections — `IEnumerable<T>` vs `ICollection<T>`

When *consuming* a collection (especially if you iterate twice or take
`.Count`), prefer `ICollection<T>` or `IReadOnlyList<T>` in the parameter
type. `IEnumerable<T>` is not safe to assume re-iterable — LINQ-to-DB,
async streams, and lazy yield-iterators enumerate exactly once.

```csharp
// risky: if items is a lazy IEnumerable, the second loop sees nothing
public void Process(IEnumerable<Item> items)
{
    foreach (var item in items) PreCheck(item);
    foreach (var item in items) Apply(item);
}

// safe
public void Process(ICollection<Item> items) { ... }
```

Reverse direction: *return* `IEnumerable<T>` (or `IAsyncEnumerable<T>`)
when the caller might want streaming; *return* `ICollection<T>` /
`IReadOnlyList<T>` when materialization is intentional.

## Configuration via `IOptions<T>`

Bind configuration to typed POCOs and inject `IOptions<TConfig>` (or
`IOptionsSnapshot<>` / `IOptionsMonitor<>`) — don't read from
`IConfiguration` directly in business logic. Compile-time shape,
validation hooks, and test-friendly stubs.

```csharp
public class FwHeadlessConfig
{
    public required string HgWebUrl { get; init; }
    public int FdoDataModelVersion { get; init; }
}

// Program.cs
services.Configure<FwHeadlessConfig>(config.GetSection("FwHeadless"));

// usage
public class Service(IOptions<FwHeadlessConfig> options)
{
    public void Do() { var url = options.Value.HgWebUrl; }
}
```

Bind integers as `int`, not `string` — the binder handles conversion.

## JSON — cache `JsonSerializerOptions`

`JsonSerializerOptions` builds reflection metadata on first use and caches
it *on the instance*. Constructing a fresh options object inside a per-
call function loses the cache and pays the reflection cost on every call.

```csharp
// good
private static readonly JsonSerializerOptions JsonOpts = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Converters = { new JsonStringEnumConverter() },
};

public string Serialize(T value) => JsonSerializer.Serialize(value, JsonOpts);

// bad — rebuilds reflection metadata on every call
public string Serialize(T value)
{
    var opts = new JsonSerializerOptions { ... };
    return JsonSerializer.Serialize(value, opts);
}
```

## DI safety — what NOT to inject

The container is mutable: any code path can re-register a service. That
makes it dangerous to put value types or behavior-changing constants in
the container — something somewhere could silently re-register and change
behavior across the app.

```csharp
// bad: someone could register NormalizationForm.FormC and break us
services.AddSingleton(NormalizationForm.FormD);

// good
public static class Normalize
{
    public const NormalizationForm Form = NormalizationForm.FormD;
}
```

Reserve DI for services. Use `IOptions<T>` for configuration values, and
`const` / `static readonly` for constants and value types.

## Threading — `Task.Run` is for escaping the UI thread

`Task.Run` schedules work on the thread pool. In ASP.NET or library code
that's already on the thread pool, calling it is unnecessary noise. The
only honest justification is escaping a UI/sync context — a MAUI
`[JSInvokable]` method runs on the UI thread, so `Task.Run` to do
background work there is correct (see PR #1893).

```csharp
// bad in API code (already on thread pool):
[HttpGet]
public Task<Foo> Get() => Task.Run(() => ComputeFoo());

// honest in MAUI:
[JSInvokable]
public Task<Foo> Get() => Task.Run(() => ComputeFoo()); // UI thread → pool
```

If you see `Task.Run` in a non-UI code path, ask why. Often it's a sign
the underlying code wasn't actually async.

## Cancellation — `CancellationToken`, not a flag

A cancellation flag on a job tells anyone awaiting it that it's
"cancelled" — but it doesn't actually stop the work. True cancellation
stores the `CancellationToken` and threads it through every async / long-
running call.

```csharp
// good
public async Task RunAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
        await DoWorkAsync(ct);
}

// bad: setting Cancelled = true doesn't actually stop anything
public async Task RunAsync()
{
    while (!Cancelled) await DoWorkAsync();
}
```

Cancellation that doesn't unwind through the full call stack isn't
cancellation.

## HTTP status codes — no "200 with error body"

REST endpoints return correct status codes. A `200 OK` containing
`{ "success": false, "error": "..." }` is broken — middleware and clients
(monitoring especially) interpret 200 as success.

Common correct codes in this codebase:
- `400 Bad Request` — malformed input.
- `401 Unauthorized` — no auth.
- `403 Forbidden` — auth present but lacks permission.
- `404 Not Found` — resource doesn't exist.
- `406 Not Acceptable` — invalid project code / unsupported content type.
- `409 Conflict` — duplicate project / version mismatch.
- `500 Internal Server Error` — server-side failure that surfaced.

The error message goes in the body; the status code carries half the
meaning. Return both.

## API stability across deployed clients

LexBox has long-lived deployed clients (FwLite, Send/Receive, web app)
that don't all upgrade simultaneously. When changing a public API
endpoint, the default is **additive**: add `*V2` and keep the old endpoint
for ~1 release of deprecation.

```csharp
// old endpoint stays
[HttpGet("listProjects")]
public Task<OldResult> ListProjects() => ...

// new endpoint added
[HttpGet("listProjectsV2")]
public Task<NewResult> ListProjectsV2() => ...
```

In the PR body: mention the deprecation plan ("remove `listProjects` in
N+1 once clients are on V2"). Hard breaks on public endpoints need
explicit sign-off.

GraphQL is more forgiving — additive changes (new fields) are safe;
renames or type changes need the same versioning thought.

## Atomic operations — save once per logical unit

A logical "save" should call `SaveChangesAsync` once. Two saves in the
same method usually mean either (a) the second can be batched into the
first, or (b) the operation isn't atomic and half can persist on failure.

```csharp
// bad
entity.Foo = newFoo;
await db.SaveChangesAsync();      // first save
entity.Bar = ComputeFromFoo();
await db.SaveChangesAsync();      // could fail leaving partial state

// good
entity.Foo = newFoo;
entity.Bar = ComputeFromFoo();
await db.SaveChangesAsync();
```

Exception: when the two saves cross DbContext boundaries (different
transactions) and the second is idempotent.

## DbContext re-use within a logical operation

Resolve `DbContext` once per logical operation, not per call. Creating
two contexts for "first lookup + then save" is a recurring finding (see
PR #1795) — it doubles query cost and risks inconsistent reads.

Look at the diff for the same `DbContext` injected/awaited twice in a
single method.

## Behavior on records (with serialization in mind)

Methods on records read better than detached static helpers — the data
structure owns its behavior. But:

```csharp
public record LfMergeBridgeResult(string Output, IProgress<int>? Progress);
// ^ Progress on the record body means it'll appear in serialized output;
//   IProgress isn't serializable, so you'll get nulls or worse.
```

When the convenience comes from non-serializable state, add a second
constructor instead of widening the record body:

```csharp
public record LfMergeBridgeResult(string Output, bool ErrorEncountered)
{
    public LfMergeBridgeResult(string output, IProgress<int> progress)
        : this(output, progress.ErrorEncountered) { }
}
```

This keeps the convenience without leaking state into serialization.

## Backend defense-in-depth

If the frontend already validates an input, the backend should also
validate it — *"in case of frontend bugs"* (PR #1829). Browser tampering,
older client versions, and direct API calls all bypass frontend checks.
Don't skip backend validation just because the UI prevents the bad case.

