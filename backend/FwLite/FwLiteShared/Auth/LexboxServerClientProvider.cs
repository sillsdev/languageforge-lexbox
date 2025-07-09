using LcmCrdt;
using MiniLcm.Project;

namespace FwLiteShared.Auth;

public class LexboxServerClientProvider(CurrentProjectService currentProjectService, OAuthClientFactory oAuthClientFactory): IServerHttpClientProvider
{
    public async ValueTask<HttpClient> GetHttpClient()
    {
        var project = await currentProjectService.GetProjectData();
        var oAuthClient = oAuthClientFactory.GetClient(project);
        var httpClient = await oAuthClient.CreateHttpClient();
        if (httpClient is null) throw new InvalidOperationException("Unable to create http client, are we logged in?");
        return httpClient;
    }
}
