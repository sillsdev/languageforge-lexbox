using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using FluentAssertions.Execution;
using Xunit.Abstractions;

namespace FwLiteProjectSync.Tests;

internal static class BenchmarkSupport
{
    /// <summary>
    /// Standard config for sync benchmarks: in-process toolchain, one warmup + four measurement
    /// iterations (Monitoring strategy), and a logger that pipes the BDN summary table into
    /// xUnit's test output.
    /// </summary>
    /// <remarks>
    /// The in-process toolchain is what lets a benchmark class read a static field set by its
    /// xUnit driver (see <c>FirstSyncBench.Fixture</c>) — a separate-process toolchain would
    /// fork a clean AppDomain and lose that reference.
    ///
    /// The default <see cref="InProcessNoEmitToolchain"/> timeout is 5 min, which is too short
    /// for multi-iteration sync benchmarks (e.g. delete-heavy alone is ~80s/iter and full-import
    /// setup adds ~50s/iter, so 5 iterations need ~11 min). 30 min covers the worst case with
    /// headroom.
    ///
    /// Monitoring + WarmupCount=1 means iteration 1 absorbs JIT + EF model build + first-touch
    /// file cache and is discarded; iterations 2-5 are measured. ColdStart would skip warmup
    /// entirely; for these slow ops the JIT/model-build noise in iteration 1 is large enough
    /// that one discarded warmup tightens StdDev meaningfully without changing total CI time.
    /// </remarks>
    public static IConfig ConfigFor(ITestOutputHelper output)
    {
        var toolchain = new InProcessNoEmitToolchain(TimeSpan.FromMinutes(30), logOutput: false);
        return ManualConfig.CreateEmpty()
            .AddJob(Job.Default
                .WithStrategy(RunStrategy.Monitoring)
                .WithWarmupCount(1)
                .WithIterationCount(4)
                .WithToolchain(toolchain))
            .AddExporter(JsonExporter.FullCompressed)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .AddLogger(new XUnitBenchmarkLogger(output));
    }

    /// <summary>
    /// Asserts the benchmark run produced usable measurements. Call inside an
    /// <see cref="AssertionScope"/> so per-report threshold failures still surface.
    /// </summary>
    public static void AssertRunWasSuccessful(Summary summary)
    {
        summary.HasCriticalValidationErrors.Should().BeFalse("BenchmarkDotNet reported critical validation errors");
        summary.Reports.Should().NotBeEmpty("BenchmarkDotNet produced no reports");
        foreach (var report in summary.Reports)
        {
            report.Success.Should().BeTrue($"benchmark {report.BenchmarkCase.DisplayInfo} should have completed without error");
            report.ResultStatistics.Should().NotBeNull($"benchmark {report.BenchmarkCase.DisplayInfo} should have produced statistics");
        }
    }
}
