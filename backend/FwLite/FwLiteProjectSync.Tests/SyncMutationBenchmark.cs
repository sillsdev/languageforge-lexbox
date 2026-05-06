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
    public void Sync_AfterMutations_Sena3()
    {
        MutationSyncBench.Fixture = fixture;
#if DEBUG
        // Debug timings are unreliable (no JIT optimizations + BDN harness flags Debug builds).
        // Run each profile once for code-path coverage; thresholds enforced in Release only (CI benchmark job).
        output.WriteLine("Debug build: running each profile once for coverage; thresholds enforced in Release only.");
        var bench = new MutationSyncBench();
        foreach (var profile in MutationSyncBench.ThresholdSecondsByProfile.Keys)
        {
            bench.Profile = profile;
            bench.IterationSetup();
            try { _ = bench.SyncAfterMutations(); }
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
    public static readonly IReadOnlyDictionary<string, double> ThresholdSecondsByProfile = new Dictionary<string, double>
    {
        // CI 2026-05-06: mean 58.3s, StdDev 4.1s (high variance) => 72s (~4σ above mean)
        ["component-heavy"] = 1,//72.0,
        // CI 2026-05-06: mean 94.3s, StdDev 5.7s (high variance) => 115s (~4σ above mean)
        ["delete-heavy"] = 1,//115.0,
        // CI 2026-05-06: mean 36.2s, StdDev 3.3s (medium variance) => 45s (~3σ above mean)
        ["mixed-realistic"] = 1,//45.0,
        // CI 2026-05-06: mean 5.05s, StdDev 0.4s (low variance) => 7s (generous margin since it's already pretty fast and we want to avoid false positives from noise).
        ["patch-heavy"] = 1,//7.0,
        // CI 2026-05-06: mean 0.77s, StdDev 0.2s (low variance) => 3s (super fast, so meh)
        ["reorder-heavy"] = 1,//3.0,
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
    public SyncResult SyncAfterMutations()
    {
        return _syncService.Sync(_project.CrdtApi, _project.FwDataApi, _projectSnapshot).Result;
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
                await ReorderSenses(api, shuffled.Take(count));
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

    // Disjoint slices so each kind of mutation operates on a different set of entries.
    private static async Task MixedRealistic(IMiniLcmApi api, List<Entry> entries, int totalCount)
    {
        var per = totalCount / 4;
        await DeleteEntries(api, entries.Take(per));
        await PatchLexemes(api, entries.Skip(per).Take(per));
        await ReorderSenses(api, entries.Skip(per * 2).Take(per));
        await AndCompleteFormComponents(api, [.. entries.Skip(per * 3)], per);
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

    private static async Task ReorderSenses(IMiniLcmApi api, IEnumerable<Entry> entries)
    {
        // Only entries with 2+ senses can be reordered; move the first sense to the end.
        foreach (var entry in entries.Where(e => e.Senses.Count >= 2))
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
            catch
            {
                // Some components are invalid. Just skip and try another.
            }
        }

        if (applied < targetCount)
        {
            throw new InvalidOperationException(
                $"Failed to apply {targetCount} complex form links after {attempts} attempts; only applied {applied}");
        }
    }
}
#pragma warning restore VSTHRD002
