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
    /// Standard config for sync benchmarks: in-process toolchain, Monitoring with one warmup +
    /// four measurement iterations, BDN output piped to xUnit.
    /// </summary>
    /// <remarks>
    /// In-process is required so benchmark classes can read static fields set by their xUnit driver
    /// (e.g. <c>FirstSyncBench.Fixture</c>) — a separate-process toolchain would lose that reference.
    /// The 30-min timeout overrides BDN's 5-min default, which is too short for our slowest profiles.
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
