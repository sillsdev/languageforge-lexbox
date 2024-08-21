using SIL.Harmony;
using LcmCrdt;
using LocalWebApp.Auth;
using LocalWebApp.Services;
using MiniLcm;
using Entry = LcmCrdt.Objects.Entry;

namespace LocalWebApp;

public class SyncService(
    DataModel dataModel,
    CrdtHttpSyncService remoteSyncServiceServer,
    AuthHelpersFactory factory,
    CurrentProjectService currentProjectService,
    ChangeEventBus changeEventBus,
    ILexboxApi lexboxApi,
    ILogger<SyncService> logger)
{
    public async Task<SyncResults> ExecuteSync()
    {
        var remoteModel = await remoteSyncServiceServer.CreateProjectSyncable(await currentProjectService.GetProjectData());
        var syncResults = await dataModel.SyncWith(remoteModel);
        await SendNotifications(syncResults);
        return syncResults;
    }

    private async Task SendNotifications(SyncResults syncResults)
    {
        //todo figure out how to make this fire and forget. Right now it blocks sync execution.
        logger.LogInformation("Sending notifications for {Count} commits", syncResults.MissingFromLocal.Length);
        foreach (var entryId in syncResults.MissingFromLocal
                     .SelectMany(c => c.Snapshots, (commit, snapshot) => snapshot.Entity)
                     .OfType<Entry>()
                     .Select(e => e.Id)
                     .Distinct())
        {
            var entry = await lexboxApi.GetEntry(entryId);
            if (entry is Entry crdtEntry)
            {
                changeEventBus.NotifyEntryUpdated(crdtEntry);
            }
            else
            {
                logger.LogWarning("Failed to get entry {EntryId}, was not a crdt entry, was {Type}",
                    entryId,
                    entry?.GetType().FullName ?? "null");
            }
        }
    }
}
