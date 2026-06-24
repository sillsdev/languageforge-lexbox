using BenchmarkDotNet.Attributes;
#if !DEBUG
using BenchmarkDotNet.Running;
using FluentAssertions.Execution;
#endif
using FwLiteProjectSync.Tests.Fixtures;
using LexCore.Sync;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;
using Xunit.Abstractions;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// Times a first sync of Sena-3 from an empty CRDT (sync, not import — we save an empty
/// snapshot first so the path runs as a real sync).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Benchmark")]
[Collection(Sena3Collection.Name)]
public class SyncBenchmark(Sena3Fixture fixture, ITestOutputHelper output)
{
    [Fact]
    public async Task First_Sync_Sena3()
    {
        FirstSyncBench.Fixture = fixture;
#if DEBUG
        // Debug timings are unreliable (no JIT optimizations); thresholds enforced in Release only.
        output.WriteLine("Debug build: running once for coverage; threshold enforced in Release only.");
        var bench = new FirstSyncBench();
        bench.IterationSetup();
        try { _ = await bench.SyncFromEmpty(); }
        finally { bench.IterationCleanup(); }
#else
        using var scope = new AssertionScope();
        var summary = BenchmarkRunner.Run<FirstSyncBench>(BenchmarkSupport.ConfigFor(output));
        BenchmarkSupport.AssertRunWasSuccessful(summary);

        var report = summary.Reports.Single();
        var meanSeconds = report.ResultStatistics!.Mean / 1_000_000_000.0;
        output.WriteLine($"first-sync mean = {meanSeconds:F2}s (bound={FirstSyncBench.ThresholdSeconds:F2}s)");

        meanSeconds.Should().BeLessThan(FirstSyncBench.ThresholdSeconds,
            $"first-sync should not regress past its threshold — see {nameof(FirstSyncBench)}.{nameof(FirstSyncBench.ThresholdSeconds)}");
#endif
    }
}

// BenchmarkDotNet doesn't support async [IterationSetup]/[IterationCleanup] signatures,
// so GetAwaiter().GetResult() below is intentional.
#pragma warning disable VSTHRD002

public class FirstSyncBench
{
    // Bound catches large regressions, not tight perf budgets — CI variance is too high for that.
    // round(mean * 1.4); rough mean from a ubuntu-latest CI run is ~57s.
    public const double ThresholdSeconds = 80;

    internal static Sena3Fixture Fixture = null!;

    private TestProject _project = null!;
    private ProjectSnapshot _projectSnapshot = null!;
    private CrdtFwdataProjectSyncService _syncService = null!;

    [IterationSetup]
    public void IterationSetup()
    {
        _project = Fixture.SetupProjects().GetAwaiter().GetResult();
        _syncService = _project.Services.GetRequiredService<CrdtFwdataProjectSyncService>();

        // Save an empty snapshot so the first sync runs as Sync, not Import.
        ProjectSnapshotService.SaveProjectSnapshot(_project.FwDataProject, ProjectSnapshot.Empty)
            .GetAwaiter().GetResult();
        _projectSnapshot = _project.Services.GetRequiredService<ProjectSnapshotService>()
            .GetProjectSnapshot(_project.FwDataProject).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Expected snapshot to exist after saving");
    }

    [Benchmark]
    public async Task<SyncResult> SyncFromEmpty()
    {
        return await _syncService.Sync(_project.CrdtApi, _project.FwDataApi, _projectSnapshot);
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _project?.Dispose();
        _project = null!;
    }
}
#pragma warning restore VSTHRD002
