using FwLiteShared.Auth;
using FwLiteShared.KeepAwake;
using System.Net;
using FwLiteShared.LocalSync;
using FwLiteShared.LocalSync.Hosting;
using LcmCrdt;
using FwLiteShared.Sync;
using LcmCrdt.Data;
using LexCore.Sync;
using SIL.Harmony;
using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public class SyncServiceJsInvokable(
    SyncService syncService,
    SyncRepository syncRepository,
    FolderSyncService folderSyncService,
    IKeepAwake keepAwake,
    CurrentProjectService currentProjectService,
    LocalSyncHosting localSyncHosting,
    LocalSyncJoinSession joinSession,
    ILocalNetworkAccess localNetworkAccess)
{
    [JSInvokable]
    public Task<ProjectSyncStatus> GetSyncStatus()
    {
        return syncService.GetSyncStatus();
    }

    [JSInvokable]
    public async Task<SyncJobResult> TriggerFwHeadlessSync()
    {
        var triggerResponse = await syncService.TriggerSync();

        // Check if trigger failed
        if (triggerResponse is null)
        {
            return new SyncJobResult(SyncJobStatusEnum.UnableToAuthenticate, "Unable to trigger sync - not authenticated");
        }

        if (!triggerResponse.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessage(triggerResponse);
            var status = triggerResponse.StatusCode switch
            {
                System.Net.HttpStatusCode.Locked => SyncJobStatusEnum.SyncBlocked,
                System.Net.HttpStatusCode.NotFound => SyncJobStatusEnum.ProjectNotFound,
                System.Net.HttpStatusCode.Forbidden => SyncJobStatusEnum.UnableToAuthenticate,
                _ => SyncJobStatusEnum.UnableToSync
            };
            return new SyncJobResult(status, errorMessage);
        }

        return await syncService.AwaitSyncFinished();
    }

    private static async Task<string> ExtractErrorMessage(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            // Try to parse as ProblemDetails JSON
            var problemDetails = System.Text.Json.JsonDocument.Parse(content);
            if (problemDetails.RootElement.TryGetProperty("detail", out var detail))
            {
                return detail.GetString() ?? content;
            }
            if (problemDetails.RootElement.TryGetProperty("title", out var title))
            {
                return title.GetString() ?? content;
            }
            return content;
        }
        catch
        {
            return $"Sync trigger failed with status {(int)response.StatusCode} {response.ReasonPhrase}";
        }
    }

    [JSInvokable]
    public Task<PendingCommits?> CountPendingCrdtCommits()
    {
        return Task.Run(async () => await syncService.CountPendingCrdtCommits());
    }

    [JSInvokable]
    public Task<DateTimeOffset?> GetLatestSyncedCommitDate()
    {
        return syncRepository.GetLatestSyncedCommitDate();
    }

    [JSInvokable]
    public Task<SyncResults> ExecuteSync(bool skipNotifications)
    {
        // Only the sync dialog calls this, so it's a sync the user asked for
        return Task.Run(async () => await syncService.ExecuteSync(skipNotifications, refreshServerProjects: true));
    }

    [JSInvokable]
    public Task<LexboxServer?> GetCurrentServer()
    {
        return syncService.GetCurrentServer();
    }

    [JSInvokable]
    public async Task<FolderSyncSummary> SyncWithFolder(string path)
    {
        var result = await keepAwake.RunAsync(new("Syncing with a folder"),
            async () => await Task.Run(async () => await folderSyncService.SyncWithFolder(path)));
        return FolderSyncSummary.From(result);
    }

    //Null when the folder is gone (e.g. the drive is unplugged), so the viewer only offers a sync that can work
    [JSInvokable]
    public string? GetRememberedSyncFolder()
    {
        var path = folderSyncService.GetRememberedFolder();
        return path is not null && Directory.Exists(path) ? path : null;
    }

    // ---- local sync over Wi-Fi: hosting ----

    [JSInvokable]
    public HostingOptions GetHostingOptions() =>
        new(localSyncHosting.CanCreateNetwork,
            [.. LocalSyncHosting.AvailableNetworks().Select(n => new AvailableNetworkView(n.Name, n.Address.ToString()))]);

    //Null when this device isn't hosting
    [JSInvokable]
    public HostingView? GetHosting() =>
        localSyncHosting.Current is { } hosting ? HostingView.From(hosting, currentProjectService.ProjectData.Id) : null;

    /// <param name="networkAddress">An address from <see cref="GetHostingOptions"/> to share over a network this device is
    /// already on; null to create this device's own Wi-Fi network.</param>
    [JSInvokable]
    public async Task<StartHostingResult> StartHosting(string? networkAddress)
    {
        HostingNetwork network = networkAddress is null
            ? new HostingNetwork.OwnAccessPoint()
            : new HostingNetwork.Existing(IPAddress.Parse(networkAddress));
        try
        {
            var hosting = await Task.Run(async () => await localSyncHosting.StartHosting(currentProjectService.Project, network));
            return new StartHostingResult(HostingView.From(hosting, currentProjectService.ProjectData.Id), null);
        }
        catch (Exception e) when (e is AccessPointUnavailableException or System.Net.Sockets.SocketException or InvalidOperationException)
        {
            return new StartHostingResult(null, e.Message);
        }
    }

    [JSInvokable]
    public async Task StopHosting() => await localSyncHosting.StopHosting();

    [JSInvokable]
    public void ApproveJoiner(string requestId) => localSyncHosting.Current?.Session.Approve(Guid.Parse(requestId));

    [JSInvokable]
    public void DeclineJoiner(string requestId) => localSyncHosting.Current?.Session.Decline(Guid.Parse(requestId));

    [JSInvokable]
    public void SetAutoApprove(bool autoApprove)
    {
        if (localSyncHosting.Current is { } hosting) hosting.Session.AutoApprove = autoApprove;
    }

    // ---- local sync over Wi-Fi: joining ----

    [JSInvokable]
    public async Task<NearbyHostView[]> FindNearbyHosts()
    {
        using var _ = localNetworkAccess.Acquire();
        var found = await LocalSyncDiscovery.Browse(TimeSpan.FromSeconds(4));
        await joinSession.SayHelloTo(found.Select(h => h.Host));
        var projectId = currentProjectService.ProjectData.Id;
        return [.. found.Select(h => new NearbyHostView(h.Host.Authority, h.ProjectName, h.ProjectId == projectId,
            h.AppVersion != AppVersion.Version || h.ProtocolVersion != LocalSyncProtocol.Version))];
    }

    [JSInvokable]
    public async Task<JoinSessionView> JoinHost(string address)
    {
        await joinSession.Join(address);
        return joinSession.View;
    }

    [JSInvokable]
    public JoinSessionView GetJoinSession() => joinSession.View;

    [JSInvokable]
    public async Task<JoinSessionView> SyncWithHostNow() => await Task.Run(async () => await joinSession.SyncNow());

    [JSInvokable]
    public async Task<JoinSessionView> LeaveHost()
    {
        await joinSession.Leave();
        return joinSession.View;
    }
}
