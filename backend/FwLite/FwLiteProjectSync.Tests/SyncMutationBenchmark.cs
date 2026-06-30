using BenchmarkDotNet.Attributes;
#if !DEBUG
using BenchmarkDotNet.Running;
using FluentAssertions.Execution;
#endif
using FwLiteProjectSync.Tests.Fixtures;
using LexCore.Sync;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using Soenneker.Utils.AutoBogus;
using Xunit.Abstractions;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// Times sync after applying a profile-specific batch of mutations to the FwData side of
/// a fully-imported Sena-3 project. Each profile stresses a different change kind.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Benchmark")]
[Collection(Sena3Collection.Name)]
public class SyncMutationBenchmark(Sena3Fixture fixture, ITestOutputHelper output)
{
    [Fact]
    public async Task Sync_AfterMutations_Sena3()
    {
        MutationSyncBench.Fixture = fixture;
#if DEBUG
        // Debug timings are unreliable (no JIT optimizations); thresholds enforced in Release only.
        output.WriteLine("Debug build: running each profile once for coverage; thresholds enforced in Release only.");
        var bench = new MutationSyncBench();
        foreach (var profile in MutationSyncBench.ThresholdSecondsByProfile.Keys)
        {
            bench.Profile = profile;
            bench.IterationSetup();
            try { _ = await bench.SyncAfterMutations(); }
            finally { bench.IterationCleanup(); }
        }
#else
        using var scope = new AssertionScope();
        var summary = BenchmarkRunner.Run<MutationSyncBench>(BenchmarkSupport.ConfigFor(output));
        BenchmarkSupport.AssertRunWasSuccessful(summary);

        foreach (var report in summary.Reports)
        {
            // ResultStatistics is guaranteed non-null by AssertRunWasSuccessful, but the scope
            // keeps going on failure so we still need a defensive null-check before dereferencing.
            if (report.ResultStatistics is null) continue;

            var profile = report.BenchmarkCase.Parameters["Profile"]?.ToString()
                ?? throw new InvalidOperationException("Expected Profile parameter on benchmark case");
            var meanSeconds = report.ResultStatistics.Mean / 1_000_000_000.0;
            var bound = MutationSyncBench.ThresholdSecondsByProfile[profile];
            output.WriteLine($"{profile} mean = {meanSeconds:F2}s (bound={bound:F2}s)");

            meanSeconds.Should().BeLessThan(bound,
                $"profile {profile} should not regress past its threshold — see {nameof(MutationSyncBench)}.{nameof(MutationSyncBench.ThresholdSecondsByProfile)}");
        }
#endif
    }
}

#pragma warning disable VSTHRD002

public class MutationSyncBench
{
    // Bounds catch large regressions, not tight perf budgets — CI variance is too high for that.
    // Each bound is round(mean * 1.4); the // comment is the rough mean from a ubuntu-latest CI run.
    public static readonly IReadOnlyDictionary<string, double> ThresholdSecondsByProfile = new Dictionary<string, double>
    {
        // ~40s
        ["component-heavy"] = 56,
        // ~63s
        ["delete-heavy"] = 88,
        // ~25s
        ["mixed-realistic"] = 35,
        // ~4.6s
        ["patch-heavy"] = 7,
        // ~1.9s
        ["reorder-heavy"] = 3,
    };

    public static IEnumerable<string> Profiles => ThresholdSecondsByProfile.Keys;

    [ParamsSource(nameof(Profiles))]
    public string Profile { get; set; } = "";

    private const int MutationCount = 400;

    private static readonly AutoFaker AutoFaker = new();

    internal static Sena3Fixture Fixture = null!;

    private TestProject _project = null!;
    private ProjectSnapshot _projectSnapshot = null!;
    private CrdtFwdataProjectSyncService _syncService = null!;

    [IterationSetup]
    public void IterationSetup()
    {
        _project = Fixture.SetupProjects().GetAwaiter().GetResult();
        var services = _project.Services;
        _syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();
        var snapshotService = services.GetRequiredService<ProjectSnapshotService>();

        _syncService.Import(_project.CrdtApi, _project.FwDataApi).GetAwaiter().GetResult();
        snapshotService.RegenerateProjectSnapshot(_project.CrdtApi, _project.FwDataProject, keepBackup: false)
            .GetAwaiter().GetResult();
        _projectSnapshot = snapshotService.GetProjectSnapshot(_project.FwDataProject).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Expected snapshot to exist after regeneration");

        ApplyProfile(Profile, _project.FwDataApi, MutationCount).GetAwaiter().GetResult();
    }

    [Benchmark]
    public async Task<SyncResult> SyncAfterMutations()
    {
        return await _syncService.Sync(_project.CrdtApi, _project.FwDataApi, _projectSnapshot);
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _project?.Dispose();
        _project = null!;
    }

    private static async Task ApplyProfile(string profile, IMiniLcmApi api, int count)
    {
        var allEntries = await api.GetAllEntries().ToListAsync();
        var shuffled = AutoFaker.Faker.Random.Shuffle(allEntries).ToList();

        switch (profile)
        {
            case "delete-heavy":
                await DeleteEntries(api, shuffled.Take(count));
                break;
            case "patch-heavy":
                await PatchLexemes(api, shuffled.Take(count));
                break;
            case "reorder-heavy":
                // Filter to reorderable entries before taking count, else we'd reorder only the
                // 2+-sense fraction that happens to land in the first `count` shuffled entries.
                await ReorderSenses(api, Reorderable(shuffled).Take(count));
                break;
            case "component-heavy":
                await AndCompleteFormComponents(api, shuffled, count);
                break;
            case "mixed-realistic":
                await MixedRealistic(api, shuffled, count);
                break;
            default:
                throw new ArgumentException($"Unknown profile: {profile}", nameof(profile));
        }
    }

    // Disjoint slices, one per mutation kind. Reorder only does anything to entries with 2+ senses,
    // so it gets first pick of those; the remaining entries are split among the other three kinds.
    private static async Task MixedRealistic(IMiniLcmApi api, List<Entry> entries, int totalCount)
    {
        var per = totalCount / 4;
        var reorderSlice = Reorderable(entries).Take(per).ToList();
        var reorderIds = reorderSlice.Select(e => e.Id).ToHashSet();
        var rest = entries.Where(e => !reorderIds.Contains(e.Id)).ToList();

        await DeleteEntries(api, rest.Take(per));
        await PatchLexemes(api, rest.Skip(per).Take(per));
        await ReorderSenses(api, reorderSlice);
        await AndCompleteFormComponents(api, [.. rest.Skip(per * 2)], per);
    }

    private static async Task DeleteEntries(IMiniLcmApi api, IEnumerable<Entry> entries)
    {
        foreach (var entry in entries)
            await api.DeleteEntry(entry.Id);
    }

    private static async Task PatchLexemes(IMiniLcmApi api, IEnumerable<Entry> entries)
    {
        foreach (var entry in entries)
        {
            var after = entry.Copy();
            // Mutate the first existing lexeme WS, or fall back to citation.
            if (after.LexemeForm.Values.Count > 0)
            {
                var (wsId, val) = after.LexemeForm.Values.First();
                after.LexemeForm[wsId] = val + "_mut";
            }
            else if (after.CitationForm.Values.Count > 0)
            {
                var (wsId, val) = after.CitationForm.Values.First();
                after.CitationForm[wsId] = val + "_mut";
            }
            else
            {
                continue;
            }
            await api.UpdateEntry(entry, after);
        }
    }

    // A reorder only changes entries with 2+ senses; callers filter to these before slicing a count.
    private static IEnumerable<Entry> Reorderable(IEnumerable<Entry> entries) =>
        entries.Where(e => e.Senses.Count >= 2);

    private static async Task ReorderSenses(IMiniLcmApi api, IEnumerable<Entry> entries)
    {
        // Entries are pre-filtered to 2+ senses (see Reorderable); move the first sense to the end.
        foreach (var entry in entries)
        {
            var first = entry.Senses[0];
            var last = entry.Senses[^1];
            if (first.Id == last.Id) continue;
            await api.MoveSense(entry.Id, first.Id, new BetweenPosition(last.Id, null));
        }
    }

    private static async Task AndCompleteFormComponents(IMiniLcmApi api, List<Entry> pool, int targetCount)
    {
        var attempted = new HashSet<(Guid, Guid)>(
            pool.SelectMany(e => e.Components.Select(c => (c.ComplexFormEntryId, c.ComponentEntryId))));

        var applied = 0;
        var attempts = 0;
        var maxAttempts = targetCount * 10;
        Exception? lastError = null;
        while (applied < targetCount && attempts < maxAttempts)
        {
            attempts++;
            var cfIdx = AutoFaker.Faker.Random.Int(0, pool.Count - 1);
            var compIdx = AutoFaker.Faker.Random.Int(0, pool.Count - 1);
            if (cfIdx == compIdx) continue;
            var cf = pool[cfIdx];
            var comp = pool[compIdx];
            if (!attempted.Add((cf.Id, comp.Id))) continue;

            try
            {
                await api.CreateComplexFormComponent(new ComplexFormComponent
                {
                    Id = Guid.NewGuid(),
                    ComplexFormEntryId = cf.Id,
                    ComponentEntryId = comp.Id,
                    ComponentSenseId = comp.Senses.Any() ? AutoFaker.Faker.PickRandom(comp.Senses).Id : null,
                });
                applied++;
            }
            catch (Exception e)
            {
                // Some randomly-paired components are invalid; skip and try another. Keep the last
                // error so a regression that rejects *every* pair is diagnosable rather than silent.
                lastError = e;
            }
        }

        if (applied < targetCount)
        {
            throw new InvalidOperationException(
                $"Failed to apply {targetCount} complex form links after {attempts} attempts; only applied {applied}",
                lastError);
        }
    }
}
#pragma warning restore VSTHRD002
