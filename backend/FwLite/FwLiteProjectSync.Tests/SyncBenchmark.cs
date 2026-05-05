using System.Diagnostics;
using System.Text.Json;
using FwDataMiniLcmBridge.Api;
using FwLiteProjectSync.Tests.Fixtures;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;
using MiniLcm.Models;
using Xunit.Abstractions;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// Performance benchmark for CRDT↔FwData sync. Runs the same scenario as
/// <see cref="Sena3SyncTests.SyncWithoutImport_CrdtShouldMatchFwdata"/> (empty CRDT → first
/// full sync against Sena-3 FwData) and records per-iteration timings to a JSON file in
/// the system temp directory so results survive a branch checkout.
///
/// Run with:
///   dotnet test backend/FwLite/FwLiteProjectSync.Tests -c Release --filter "Category=Benchmark" \
///       -e BENCH_CONFIG=baseline -e BENCH_ITERATIONS=3 -e BENCH_WARMUP=1
/// </summary>
[Trait("Category", "Benchmark")]
public class SyncBenchmark : IClassFixture<Sena3Fixture>
{
    private readonly Sena3Fixture _fixture;
    private readonly ITestOutputHelper _output;

    public SyncBenchmark(Sena3Fixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task SyncWithoutImport_Benchmark()
    {
#if DEBUG
        throw new InvalidOperationException(
            "This benchmark must be run in Release configuration. Use `dotnet test -c Release`.");
#else
        var config = Environment.GetEnvironmentVariable("BENCH_CONFIG") ?? "unlabeled";
        var warmupIterations = int.Parse(Environment.GetEnvironmentVariable("BENCH_WARMUP") ?? "1");
        var measureIterations = int.Parse(Environment.GetEnvironmentVariable("BENCH_ITERATIONS") ?? "3");
        var totalIters = warmupIterations + measureIterations;

        _output.WriteLine($"=== SyncBenchmark config={config} warmup={warmupIterations} measure={measureIterations} ===");

        var times = new List<double>();
        int crdtChanges = 0;
        int fwdataChanges = 0;
        int entryCount = 0;

        for (int i = 0; i < totalIters; i++)
        {
            var setupSw = Stopwatch.StartNew();
            using var project = await _fixture.SetupProjects();
            var services = project.Services;
            var syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();
            var snapshotService = services.GetRequiredService<ProjectSnapshotService>();
            var fwDataApi = project.FwDataApi;
            var crdtApi = project.CrdtApi;

            // Save an empty ProjectSnapshot to disk so the sync path runs (not Import path)
            await ProjectSnapshotService.SaveProjectSnapshot(fwDataApi.Project, ProjectSnapshot.Empty);
            var projectSnapshot = await snapshotService.GetProjectSnapshot(fwDataApi.Project)
                ?? throw new InvalidOperationException("Expected snapshot to exist after saving");

            entryCount = fwDataApi.EntryCount;
            var setupElapsed = setupSw.Elapsed;

            var syncSw = Stopwatch.StartNew();
            var result = await syncService.Sync(crdtApi, fwDataApi, projectSnapshot);
            var syncElapsed = syncSw.Elapsed;

            var label = i < warmupIterations ? $"warmup {i + 1}/{warmupIterations}" : $"iter {i - warmupIterations + 1}/{measureIterations}";
            _output.WriteLine($"[{label}] setup={setupElapsed.TotalSeconds:F2}s sync={syncElapsed.TotalSeconds:F2}s crdt={result.CrdtChanges} fw={result.FwdataChanges}");

            if (i >= warmupIterations)
            {
                times.Add(syncElapsed.TotalMilliseconds);
                crdtChanges = result.CrdtChanges;
                fwdataChanges = result.FwdataChanges;
            }
        }

        var meanMs = times.Average();
        var minMs = times.Min();
        var maxMs = times.Max();
        _output.WriteLine("=== Results ===");
        _output.WriteLine($"config:       {config}");
        _output.WriteLine($"entries:      {entryCount}");
        _output.WriteLine($"crdtChanges:  {crdtChanges}");
        _output.WriteLine($"fwdataChanges:{fwdataChanges}");
        _output.WriteLine($"mean (sync):  {meanMs / 1000.0:F2}s");
        _output.WriteLine($"min  (sync):  {minMs / 1000.0:F2}s");
        _output.WriteLine($"max  (sync):  {maxMs / 1000.0:F2}s");

        var resultDir = Path.Combine(Path.GetTempPath(), "fwlite-bench-results");
        Directory.CreateDirectory(resultDir);
        var resultPath = Path.Combine(resultDir, $"{DateTime.Now:yyyyMMdd-HHmmss}-{config}.json");
        var payload = new
        {
            config,
            entries = entryCount,
            crdtChanges,
            fwdataChanges,
            mean_ms = meanMs,
            min_ms = minMs,
            max_ms = maxMs,
            times_ms = times.ToArray(),
            timestamp = DateTime.UtcNow.ToString("O")
        };
        await File.WriteAllTextAsync(resultPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine($"Results written to {resultPath}");
#endif
    }
}
