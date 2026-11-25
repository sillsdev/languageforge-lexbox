using System.Collections.Concurrent;
using FwLiteShared.Sync;
using LcmCrdt;
using LcmCrdt.Utils;
using Microsoft.EntityFrameworkCore;
using SIL.Harmony;
using SIL.Harmony.Core;
using SIL.Harmony.Db;

namespace FwLiteShared.LocalSync;

/// <summary>
/// The commit-sync side of a local sync host, independent of the HTTP server it sits behind. It serves only
/// the project it's resolved for (the hosted project), mirroring Lexbox's <c>/api/crdt/{id}/...</c> routes so
/// joiners can reuse <see cref="LcmCrdt.RemoteSync.CrdtHttpSyncService"/>. Approval is checked before these
/// handlers are reached.
/// </summary>
public class LocalSyncHost(
    DataModel dataModel,
    CurrentProjectService currentProjectService,
    IDbContextFactory<LcmCrdtDbContext> dbContextFactory,
    SyncResultsProcessor syncResultsProcessor)
{
    // Handlers are resolved per request, so the lock is shared by hosted project Id.
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> AddLocks = new();

    public async Task<SyncState> GetSyncState(Guid projectId)
    {
        AssertHosted(projectId);
        return await dataModel.GetSyncState();
    }

    public async Task<ChangesResult<Commit>> GetChanges(Guid projectId, SyncState otherHeads)
    {
        AssertHosted(projectId);
        return await dataModel.GetChanges(otherHeads);
    }

    public async Task AddRange(Guid projectId, IEnumerable<Commit> commits)
    {
        AssertHosted(projectId);
        // A joiner may resend commits we already applied (e.g. retrying after a timeout), possibly while the first
        // request is still running. Processing those again would mark comments the host has already read as unread,
        // so filtering and applying happen under one lock.
        var addLock = AddLocks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
        await addLock.WaitAsync();
        try
        {
            var newCommits = await WithoutKnownCommits([.. commits]);
            if (newCommits.Length == 0) return;
            // the joiner's Lexbox upload date isn't ours; keeping it would report a Lexbox sync this device never did
            foreach (var commit in newCommits) commit.SetSyncDate(null);
            await ((ISyncable)dataModel).AddRangeFromSync(newCommits);
            var results = new SyncResults(newCommits, [], true);
            await syncResultsProcessor.ApplyCommentReadStatus(results);
            await syncResultsProcessor.SendNotifications(results);
        }
        finally
        {
            addLock.Release();
        }
    }

    private async Task<Commit[]> WithoutKnownCommits(Commit[] commits)
    {
        var ids = commits.Select(c => c.Id).ToArray();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var known = await ((ICrdtDbContext)dbContext).Commits
            .Where(c => ids.Contains(c.Id))
            .Select(c => c.Id)
            .ToHashSetAsync();
        return [.. commits.Where(c => !known.Contains(c.Id))];
    }

    private void AssertHosted(Guid projectId)
    {
        var hostedId = currentProjectService.ProjectData.Id;
        if (projectId != hostedId) throw new LocalSyncProjectNotFoundException(projectId, hostedId);
    }
}

public class LocalSyncProjectNotFoundException(Guid requestedProjectId, Guid hostedProjectId)
    : Exception($"Project {requestedProjectId} is not hosted here; this host is sharing project {hostedProjectId}");
