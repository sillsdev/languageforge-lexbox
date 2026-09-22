# In-repo Roslyn analyzers

Custom static-analysis rules that live alongside the code they police and are consumed by every
backend project automatically — surfacing as `dotnet build`/CI diagnostics **and** IDE squiggles with
no per-developer setup. Distribution is via `ProjectReference`, not a published NuGet package.

## Layout

| Project | Purpose |
|---|---|
| `LexboxAnalyzers` | The analyzer assembly (`netstandard2.0`). One rule per file under `Rules/`. |
| `LexboxAnalyzers.Tests` | xUnit tests using `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`. |

## How it's wired

- `backend/Directory.Build.targets` adds `LexboxAnalyzers` to every backend project as
  `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`, except projects that set
  `IsAnalyzerProject=true` (the analyzer and its test project) — that guard prevents a build cycle.
- Severities are set in the repo-root `.editorconfig` (`dotnet_diagnostic.<ID>.severity`), **not** in
  the descriptor. `defaultSeverity` in code is only a fallback.

## Rules

| ID | Category | Description |
|---|---|---|
| `LX0001` | `LcmCrdt.Reliability` | Concrete Harmony change types (`SIL.Harmony.Changes.IChange`) must declare a `Guid entityId` constructor. Configured as **error**. |

## Adding a rule

1. Allocate an ID in `DiagnosticIds.cs` (`LX` + four digits; never reuse or renumber).
2. Add a `Rules/*.cs` analyzer — prefer `IOperation`/symbol comparison over syntax matching; resolve
   symbols in `RegisterCompilationStartAction`; include `ConfigureGeneratedCodeAnalysis(None)` and
   `EnableConcurrentExecution()`.
3. Add positive, negative near-miss, and generated-code tests.
4. Set the `.editorconfig` severity (start at `suggestion`/`warning` for retroactive rules).

We don't maintain analyzer release-tracking files (`AnalyzerReleases.*.md`); RS2008 is suppressed in
the analyzer csproj. Release tracking exists to manage compatibility for external package consumers,
which this repo doesn't have.

## Gotcha: IDE staleness

After changing analyzer logic, rebuild the analyzer and restart the Roslyn process
(`dotnet build-server shutdown`; Rider: "Restart Roslyn Analyzer Process") — the IDE caches the loaded
assembly.
