using System.Diagnostics;
using System.Text.Json;
using FwLiteProjectSync.Tests.Fixtures;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using Xunit.Abstractions;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// Multi-profile benchmark for the CRDT↔FwData *second* sync. Unlike
/// <see cref="SyncBenchmark"/> (empty-CRDT first sync, all creates), this test does a
/// full import first, then applies a workload-specific mutation to the FwData side, then
/// measures the sync that propagates those mutations into CRDT.
///
/// Different <c>IChange</c> kinds have wildly different costs:
/// <list type="bullet">
///   <item><b>DeleteChange&lt;T&gt;</b> — expensive. Cascades through
///     <c>MarkDeleted → GetSnapshotsReferencing → CurrentSnapshots</c> CTE
///     (~1.9s per delete on orc-flex before H1 preload).</item>
///   <item><b>JsonPatchChange&lt;T&gt;</b> — cheap. Pure patch apply.</item>
///   <item><b>AddEntryComponentChange</b> — medium. Exercises OrderPicker
///     (batch-aware / R4) and EntrySync two-phase (R6).</item>
///   <item><b>SetOrderChange&lt;T&gt;</b> — cheap but hits OrderPicker.</item>
/// </list>
///
/// Each profile stresses a different regime:
/// <list type="bullet">
///   <item><c>delete-heavy</c> — isolates H1 preload value.</item>
///   <item><c>patch-heavy</c> — isolates batching transaction-overhead win.</item>
///   <item><c>reorder-heavy</c> — stresses OrderPicker / BatchOrderScope (R4).</item>
///   <item><c>cflink-heavy</c> — stresses R4 + R6 (add-only; remove would dilute into delete-heavy).</item>
///   <item><c>mixed-realistic</c> — blend; anchors a top-line speedup claim.</item>
/// </list>
///
/// Run with:
///   BENCH_PROFILE=delete-heavy BENCH_CONFIG=baseline BENCH_WARMUP=0 BENCH_ITERATIONS=1 \
///     dotnet test ... --filter FullyQualifiedName=FwLiteProjectSync.Tests.SyncMutationBenchmark.Sync_MutationProfile_Benchmark
/// </summary>
[Trait("Category", "Benchmark")]
public class SyncMutationBenchmark : IClassFixture<Sena3Fixture>
{
    private readonly Sena3Fixture _fixture;
    private readonly ITestOutputHelper _output;
    private const int RandomSeed = 42;

    public SyncMutationBenchmark(Sena3Fixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Sync_MutationProfile_Benchmark()
    {
#if DEBUG
        throw new InvalidOperationException(
            "This benchmark must be run in Release configuration. Use `dotnet test -c Release`.");
#else
        var profile = Environment.GetEnvironmentVariable("BENCH_PROFILE") ?? "delete-heavy";
        var config = Environment.GetEnvironmentVariable("BENCH_CONFIG") ?? "unlabeled";
        var warmup = int.Parse(Environment.GetEnvironmentVariable("BENCH_WARMUP") ?? "0");
        var iterations = int.Parse(Environment.GetEnvironmentVariable("BENCH_ITERATIONS") ?? "1");

        // Single knob so profiles are directly comparable ("400 of each change type").
        // mixed-realistic splits this count across its 4 sub-types (N/4 each) so total work matches.
        var mutationCount = int.Parse(Environment.GetEnvironmentVariable("BENCH_MUTATION_COUNT") ?? "400");

        _output.WriteLine($"=== SyncMutationBenchmark profile={profile} config={config} count={mutationCount} warmup={warmup} iter={iterations} ===");

        var times = new List<double>();
        int crdtChanges = 0, fwdataChanges = 0, appliedMutations = 0;

        for (int i = 0; i < warmup + iterations; i++)
        {
            using var project = await _fixture.SetupProjects();
            var services = project.Services;
            var syncService = services.GetRequiredService<CrdtFwdataProjectSyncService>();
            var snapshotService = services.GetRequiredService<ProjectSnapshotService>();
            var fwDataApi = project.FwDataApi;
            var crdtApi = project.CrdtApi;

            var importSw = Stopwatch.StartNew();
            await syncService.Import(crdtApi, fwDataApi);
            await snapshotService.RegenerateProjectSnapshot(crdtApi, fwDataApi.Project, keepBackup: false);
            var snapshot = await snapshotService.GetProjectSnapshot(fwDataApi.Project)
                ?? throw new InvalidOperationException("Expected snapshot after regeneration");
            var importElapsed = importSw.Elapsed;

            var mutateSw = Stopwatch.StartNew();
            appliedMutations = await ApplyProfile(profile, fwDataApi, mutationCount);
            var mutateElapsed = mutateSw.Elapsed;

            var syncSw = Stopwatch.StartNew();
            var result = await syncService.Sync(crdtApi, fwDataApi, snapshot);
            var syncElapsed = syncSw.Elapsed;

            var label = i < warmup ? $"warmup {i + 1}/{warmup}" : $"iter {i - warmup + 1}/{iterations}";
            _output.WriteLine($"[{label}] import={importElapsed.TotalSeconds:F2}s mutate={mutateElapsed.TotalSeconds:F2}s " +
                              $"sync={syncElapsed.TotalSeconds:F2}s mutations={appliedMutations} " +
                              $"crdt={result.CrdtChanges} fw={result.FwdataChanges}");

            if (i >= warmup)
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
        _output.WriteLine($"profile:        {profile}");
        _output.WriteLine($"config:         {config}");
        _output.WriteLine($"mutations:      {appliedMutations}");
        _output.WriteLine($"crdtChanges:    {crdtChanges}");
        _output.WriteLine($"fwdataChanges:  {fwdataChanges}");
        _output.WriteLine($"mean (sync):    {meanMs / 1000.0:F2}s");
        _output.WriteLine($"min  (sync):    {minMs / 1000.0:F2}s");
        _output.WriteLine($"max  (sync):    {maxMs / 1000.0:F2}s");

        var resultDir = Path.Combine(Path.GetTempPath(), "fwlite-bench-results");
        Directory.CreateDirectory(resultDir);
        var resultPath = Path.Combine(resultDir, $"{DateTime.Now:yyyyMMdd-HHmmss}-{profile}-{config}.json");
        var payload = new
        {
            profile,
            config,
            mutations = appliedMutations,
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

    private static async Task<int> ApplyProfile(string profile, MiniLcm.IMiniLcmApi fwDataApi, int count)
    {
        var rng = new Random(RandomSeed);
        var allEntries = await fwDataApi.GetAllEntries().ToListAsync();
        // Deterministic shuffle so runs are reproducible across branches
        ShuffleInPlace(allEntries, rng);

        return profile switch
        {
            "delete-heavy" => await DeleteEntries(fwDataApi, allEntries, count),
            "patch-heavy" => await PatchLexemes(fwDataApi, allEntries, count),
            "reorder-heavy" => await ReorderSenses(fwDataApi, allEntries, count),
            "cflink-heavy" => await AddComplexFormLinks(fwDataApi, allEntries, count, rng),
            "mixed-realistic" => await MixedRealistic(fwDataApi, allEntries, count, rng),
            _ => throw new ArgumentException($"Unknown profile: {profile}")
        };
    }

    private static async Task<int> MixedRealistic(MiniLcm.IMiniLcmApi api, List<Entry> entries, int totalCount, Random rng)
    {
        // Split totalCount across 4 kinds with disjoint entry slices so mutations don't interact.
        // Each kind gets totalCount/4; the cflink slice gets all remaining entries as pool for pair picking.
        var per = totalCount / 4;
        var applied = 0;
        applied += await DeleteEntries(api, entries.Take(per).ToList(), per);
        applied += await PatchLexemes(api, entries.Skip(per).Take(per).ToList(), per);
        applied += await ReorderSenses(api, entries.Skip(per * 2).Take(per).ToList(), per);
        // cflink needs a larger pool than target count to find valid (cf, component) pairs
        applied += await AddComplexFormLinks(api, entries.Skip(per * 3).ToList(), per, rng);
        return applied;
    }

    private static async Task<int> DeleteEntries(MiniLcm.IMiniLcmApi api, List<Entry> entries, int targetCount)
    {
        var toDelete = entries.Take(Math.Min(targetCount, entries.Count)).ToList();
        foreach (var entry in toDelete)
        {
            await api.DeleteEntry(entry.Id);
        }
        return toDelete.Count;
    }

    private static async Task<int> PatchLexemes(MiniLcm.IMiniLcmApi api, List<Entry> entries, int targetCount)
    {
        var applied = 0;
        foreach (var entry in entries.Take(targetCount))
        {
            var before = entry;
            var after = entry.Copy();
            // Pick the first existing lexeme WS and mutate it; if none, fall back to citation.
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
            await api.UpdateEntry(before, after);
            applied++;
        }
        return applied;
    }

    private static async Task<int> ReorderSenses(MiniLcm.IMiniLcmApi api, List<Entry> entries, int targetCount)
    {
        // Only entries with 2+ senses can be reordered meaningfully. Take the first N such entries.
        var candidates = entries.Where(e => e.Senses.Count >= 2).Take(targetCount).ToList();
        var applied = 0;
        foreach (var entry in candidates)
        {
            // Swap positions: move first sense to the end (between last, null).
            var first = entry.Senses[0];
            var last = entry.Senses[^1];
            if (first.Id == last.Id) continue;
            await api.MoveSense(entry.Id, first.Id, new BetweenPosition(last.Id, null));
            applied++;
        }
        return applied;
    }

    private static async Task<int> AddComplexFormLinks(MiniLcm.IMiniLcmApi api, List<Entry> entries, int targetCount, Random rng)
    {
        // Add `targetCount` new complex-form → component links between pairs of distinct entries
        // that don't already share a link. Skip pairs that would create self-references or duplicates.
        var applied = 0;
        var existing = new HashSet<(Guid, Guid)>();
        foreach (var e in entries)
        {
            foreach (var c in e.Components)
                existing.Add((c.ComplexFormEntryId, c.ComponentEntryId));
        }

        var attempts = 0;
        var maxAttempts = targetCount * 10;
        while (applied < targetCount && attempts < maxAttempts)
        {
            attempts++;
            var cfIdx = rng.Next(entries.Count);
            var compIdx = rng.Next(entries.Count);
            if (cfIdx == compIdx) continue;
            var cf = entries[cfIdx];
            var comp = entries[compIdx];
            if (existing.Contains((cf.Id, comp.Id))) continue;

            try
            {
                await api.CreateComplexFormComponent(new ComplexFormComponent
                {
                    Id = Guid.NewGuid(),
                    ComplexFormEntryId = cf.Id,
                    ComponentEntryId = comp.Id,
                });
                existing.Add((cf.Id, comp.Id));
                applied++;
            }
            catch
            {
                // FwData may reject some pairs (e.g., component is complex form of itself indirectly).
                // Skip and try another pair.
            }
        }
        return applied;
    }

    private static void ShuffleInPlace<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
