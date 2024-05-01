using CrdtLib;
using LcmCrdt;

namespace LocalWebApp;

public class SyncService(
    DataModel dataModel,
    CrdtHttpSyncService remoteSyncServiceServer,
    CurrentProjectService currentProjectService)
{
    public async Task<SyncResults> ExecuteSync()
    {
        var remoteModel = remoteSyncServiceServer.CreateProjectSyncable(await currentProjectService.GetProjectData());
        return await dataModel.SyncWith(remoteModel);
    }
}
