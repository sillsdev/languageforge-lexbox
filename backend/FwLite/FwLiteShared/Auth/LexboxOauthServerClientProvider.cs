using LcmCrdt;
using Microsoft.Extensions.Options;
using MiniLcm.Project;

namespace FwLiteShared.Auth;

public class LexboxOauthServerClientProvider(
    CurrentProjectService currentProjectService,
    OAuthClientFactory oAuthClientFactory,
    IOptions<AuthConfig> authOptions): IServerHttpClientProvider
{
    public async ValueTask<HttpClient> GetHttpClient()
    {
        var project = await currentProjectService.GetProjectData();
        var oAuthClient = oAuthClientFactory.GetClient(project);
        var httpClient = await oAuthClient.CreateHttpClient();
        if (httpClient is null) throw new InvalidOperationException("Unable to create http client, are we logged in?");
        return httpClient;
    }

    public async ValueTask<ConnectionStatus> ConnectionStatus(bool forceRefresh = false)
    {
        var project = await currentProjectService.GetProjectData();
        if (string.IsNullOrEmpty(project.OriginDomain)) return MiniLcm.Project.ConnectionStatus.NoServer;
        if (!authOptions.Value.TryGetServer(project, out var server)) return MiniLcm.Project.ConnectionStatus.NoServer;

        //todo cache the result of this
        var oAuthClient = oAuthClientFactory.GetClient(server);
        var httpClient = await oAuthClient.CreateHttpClient();
        if (httpClient is null) return MiniLcm.Project.ConnectionStatus.NotLoggedIn;
        //todo take code from CrdtHttpSyncService and use it here, then make that code depend on this
        return MiniLcm.Project.ConnectionStatus.Online;
    }
}
