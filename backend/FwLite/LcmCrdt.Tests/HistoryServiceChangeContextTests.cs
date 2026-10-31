using System.Diagnostics;
using LcmCrdt.Changes;
using Microsoft.EntityFrameworkCore;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Db;
using SystemTextJsonPatch;
using Xunit.Abstractions;

namespace LcmCrdt.Tests;

/// <summary>
/// <see cref="HistoryService.LoadChangeContext"/> gives the activity preview the entity before and after a change.
/// </summary>
public class HistoryServiceChangeContextTests(ITestOutputHelper output) : HistoryServiceActivityTestsBase
{
    [Fact]
    public async Task LoadChangeContext_Edit_HasBeforeAndAfter()
    {
        var entryId = await CreateEntry("run");
        var rename = await Rename(entryId, "jog");

        var context = await Service.LoadChangeContext(rename.Id, 0);

        context.EntityType.Should().Be(nameof(Entry));
        ((Entry)context.PreviousSnapshot!).LexemeForm["en"].Should().Be("run");
        ((Entry)context.Snapshot!).LexemeForm["en"].Should().Be("jog");
    }

    [Fact]
    public async Task LoadChangeContext_Create_HasNoBefore()
    {
        var entryId = await CreateEntry("run");
        var create = await CommitsOf(entryId).FirstAsync();

        var context = await Service.LoadChangeContext(create.Id, 0);

        context.EntityType.Should().Be(nameof(Entry));
        context.PreviousSnapshot.Should().BeNull();
        ((Entry)context.Snapshot!).LexemeForm["en"].Should().Be("run");
    }

    [Fact]
    public async Task LoadChangeContext_Delete_HasNoAfter()
    {
        var entryId = await CreateEntry("run");
        var delete = await DataModel.AddChange(ClientId, new DeleteChange<Entry>(entryId), Meta());

        var context = await Service.LoadChangeContext(delete.Id, 0);

        context.EntityType.Should().Be(nameof(Entry));
        var before = (Entry)context.PreviousSnapshot!;
        before.DeletedAt.Should().BeNull();
        before.LexemeForm["en"].Should().Be("run");
        // the snapshot at a delete is the deleted entity, which the preview shows as having no after
        ((Entry)context.Snapshot!).DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadChangeContext_SenseEdit_HasBeforeAndAfter()
    {
        var entryId = await CreateEntry("run");
        var senseId = await CreateSense(entryId, "move fast", 1);
        var patch = new JsonPatchDocument<Sense>();
        patch.Replace(s => s.Gloss["en"], "go quickly");
        var edit = await DataModel.AddChange(ClientId, new JsonPatchChange<Sense>(senseId, patch), Meta());

        var context = await Service.LoadChangeContext(edit.Id, 0);

        context.EntityType.Should().Be(nameof(Sense));
        ((Sense)context.PreviousSnapshot!).Gloss["en"].Should().Be("move fast");
        ((Sense)context.Snapshot!).Gloss["en"].Should().Be("go quickly");
    }

    /// <summary>
    /// The edits' snapshots are pruned among thousands of commits on other entities, as after a large sync, yet the
    /// content is right and no read has more than a checkpoint interval of changes to replay.
    /// </summary>
    [Fact]
    public async Task LoadChangeContext_PrunedSnapshots_ReplayIsBoundedByCheckpointInterval()
    {
        const int otherCommitsEachSide = 5_000;
        const int edits = 5;
        var entryId = await CreateEntry("v0");
        var otherEntryIds = new List<Guid>();
        for (var i = 0; i < 50; i++) otherEntryIds.Add(await CreateEntry($"other {i}"));

        // one change per commit, all in one batch, like a sync bringing in a large history
        var changes = new List<IChange>();
        changes.AddRange(Enumerable.Range(0, otherCommitsEachSide).Select(i => RenameChange(otherEntryIds[i % otherEntryIds.Count], $"before {i}")));
        changes.AddRange(Enumerable.Range(1, edits).Select(i => RenameChange(entryId, $"v{i}")));
        changes.AddRange(Enumerable.Range(0, otherCommitsEachSide).Select(i => RenameChange(otherEntryIds[i % otherEntryIds.Count], $"after {i}")));
        var stopwatch = Stopwatch.StartNew();
        await DataModel.AddManyChanges(ClientId, changes, Meta, changesPerCommitMax: 1);
        output.WriteLine($"Added {changes.Count:N0} commits in {stopwatch.ElapsedMilliseconds:N0} ms");

        ICrdtDbContext db = Fixture.DbContext;
        var editCommits = await CommitsOf(entryId).Skip(1).ToListAsync();
        editCommits.Should().HaveCount(edits);
        var persistedAt = await db.Snapshots.AsNoTracking()
            .Where(s => s.EntityId == entryId)
            .Select(s => s.CommitId)
            .ToListAsync();
        // a checkpoint can keep at most one of the superseded snapshots, so at least two consecutive edits lost theirs
        var target = Enumerable.Range(1, edits - 2)
            .First(i => !persistedAt.Contains(editCommits[i - 1].Id) && !persistedAt.Contains(editCommits[i].Id));

        // warm up EF's query compilation, so the timing below is the read itself
        await Service.LoadChangeContext(editCommits[target - 1].Id, 0);
        stopwatch.Restart();
        var context = await Service.LoadChangeContext(editCommits[target].Id, 0);
        var loadMs = stopwatch.ElapsedMilliseconds;

        ((Entry)context.PreviousSnapshot!).LexemeForm["en"].Should().Be($"v{target}");
        ((Entry)context.Snapshot!).LexemeForm["en"].Should().Be($"v{target + 1}");

        // What bounds the replay: every commit is a checkpoint or fewer than the interval's changes after one.
        var maxChanges = Fixture.HarmonyConfig.MaxChangesBetweenSnapshotCheckpoints;
        var commits = await db.Commits.AsNoTracking().DefaultOrder()
            .Select(c => new { c.IsSnapshotCheckpoint, Changes = c.ChangeEntities.Count })
            .ToListAsync();
        var sinceCheckpoint = 0;
        var mostSinceCheckpoint = 0;
        foreach (var commit in commits)
        {
            sinceCheckpoint = commit.IsSnapshotCheckpoint ? 0 : sinceCheckpoint + commit.Changes;
            mostSinceCheckpoint = Math.Max(mostSinceCheckpoint, sinceCheckpoint);
        }
        output.WriteLine($"{commits.Count:N0} commits, {commits.Count(c => c.IsSnapshotCheckpoint):N0} checkpoints, " +
                         $"at most {mostSinceCheckpoint} changes to replay; LoadChangeContext took {loadMs:N0} ms");
        commits.Count.Should().BeGreaterThan(2 * otherCommitsEachSide);
        mostSinceCheckpoint.Should().BeLessThan(maxChanges);

        // a guard against gross regressions only: the bound above is the evidence, wall time varies by machine
        loadMs.Should().BeLessThan(200);
    }

    private async Task<Commit> Rename(Guid entryId, string headword)
    {
        return await DataModel.AddChange(ClientId, RenameChange(entryId, headword), Meta());
    }

    private static JsonPatchChange<Entry> RenameChange(Guid entryId, string headword)
    {
        var rename = new JsonPatchDocument<Entry>();
        rename.Replace(e => e.LexemeForm["en"], headword);
        return new JsonPatchChange<Entry>(entryId, rename);
    }

    /// <summary>the commits that changed the entity, oldest first</summary>
    private IQueryable<Commit> CommitsOf(Guid entityId)
    {
        ICrdtDbContext db = Fixture.DbContext;
        return db.Commits.AsNoTracking()
            .Where(c => c.ChangeEntities.Any(ce => ce.EntityId == entityId))
            .DefaultOrder();
    }
}
