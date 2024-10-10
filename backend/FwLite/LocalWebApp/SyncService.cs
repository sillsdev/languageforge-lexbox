using SIL.Harmony;
using LcmCrdt;
using LocalWebApp.Auth;
using LocalWebApp.Services;
using MiniLcm;
using MiniLcm.Models;
using SIL.Harmony.Entities;

namespace LocalWebApp;

public class SyncService(
    DataModel dataModel,
    CrdtHttpSyncService remoteSyncServiceServer,
    AuthHelpersFactory factory,
    CurrentProjectService currentProjectService,
    ChangeEventBus changeEventBus,
    IMiniLcmApi lexboxApi,
    ILogger<SyncService> logger)
{
    public async Task<SyncResults> ExecuteSync()
    {
        var remoteModel = await remoteSyncServiceServer.CreateProjectSyncable(await currentProjectService.GetProjectData());
        var syncResults = await dataModel.SyncWith(remoteModel);
        //need to await this, otherwise the database connection will be closed before the notifications are sent
        await SendNotifications(syncResults);
        return syncResults;
    }

    private async Task SendNotifications(SyncResults syncResults)
    {
        await foreach (var entryId in syncResults.MissingFromLocal
                     .SelectMany(c => c.Snapshots, (commit, snapshot) => snapshot.Entity)
                     .ToAsyncEnumerable()
                     .SelectAwait(async e => await GetEntryId(e.DbObject as IObjectWithId))
                     .Distinct())
        {
            if (entryId is null) continue;
            var entry = await lexboxApi.GetEntry(entryId.Value);
            if (entry is not null)
            {
                changeEventBus.NotifyEntryUpdated(entry);
            }
            else
            {
                logger.LogError("Failed to get entry {EntryId}, was not found", entryId);
            }
        }
    }

    private async ValueTask<Guid?> GetEntryId(IObjectWithId? entity)
    {
        return entity switch
        {
            Entry entry => entry.Id,
            Sense sense => sense.EntryId,
            ExampleSentence exampleSentence => (await dataModel.GetLatest<Sense>(exampleSentence.SenseId))?.EntryId,
            _ => null
        };
    }
}
