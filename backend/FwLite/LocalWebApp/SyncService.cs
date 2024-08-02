using SIL.Harmony;
using LcmCrdt;
using LocalWebApp.Auth;

namespace LocalWebApp;

public class SyncService(
    DataModel dataModel,
    CrdtHttpSyncService remoteSyncServiceServer,
    AuthHelpersFactory factory,
    CurrentProjectService currentProjectService)
{
    public async Task<SyncResults> ExecuteSync()
    {
        var remoteModel = await remoteSyncServiceServer.CreateProjectSyncable(await currentProjectService.GetProjectData());
        return await dataModel.SyncWith(remoteModel);
    }
}
