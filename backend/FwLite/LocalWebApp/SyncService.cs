using SIL.Harmony;
using LcmCrdt;
using LcmCrdt.Objects;
using LocalWebApp.Auth;
using LocalWebApp.Services;

namespace LocalWebApp;

public class SyncService(
    DataModel dataModel,
    CrdtHttpSyncService remoteSyncServiceServer,
    AuthHelpersFactory factory,
    CurrentProjectService currentProjectService,
    ChangeEventBus changeEventBus)
{
    public async Task<SyncResults> ExecuteSync()
    {
        var remoteModel = await remoteSyncServiceServer.CreateProjectSyncable(await currentProjectService.GetProjectData());
        var syncResults = await dataModel.SyncWith(remoteModel);
        foreach (var entry in syncResults.MissingFromLocal
                     .SelectMany(c => c.Snapshots, (commit, snapshot) => snapshot.Entity).OfType<Entry>()
                     .DistinctBy(e => e.Id))
        {
            changeEventBus.NotifyEntryUpdated(entry);
        }
        return syncResults;
    }
}
