using LcmCrdt;
using LcmCrdt.RemoteSync;
using SIL.Harmony;

namespace FwHeadless.Services;

public class CrdtSyncService(
    CrdtHttpSyncService httpSyncService,
    IHttpClientFactory httpClientFactory,
    CurrentProjectService currentProjectService,
    DataModel dataModel,
    ILogger<CrdtSyncService> logger)
{
    public virtual async Task SyncHarmonyProject()
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_id", currentProjectService.ProjectData.Id);
        var lexboxRemoteServer = await httpSyncService.CreateProjectSyncable(
            currentProjectService.ProjectData,
            httpClientFactory.CreateClient(FwHeadlessKernel.LexboxHttpClientName)
        );
        var syncResults = await dataModel.SyncWith(lexboxRemoteServer);
        if (!syncResults.IsSynced) throw new InvalidOperationException("Sync failed");
        logger.LogInformation(
            "Synced with Lexbox, Downloaded changes: {MissingFromLocal}, Uploaded changes: {MissingFromRemote}",
            syncResults.MissingFromLocal.Length,
            syncResults.MissingFromRemote.Length);
    }
}
