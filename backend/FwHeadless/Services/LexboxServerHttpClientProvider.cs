using MiniLcm.Project;

namespace FwHeadless.Services;

public class LexboxServerHttpClientProvider(IHttpClientFactory httpClientFactory) : IServerHttpClientProvider
{
    public ValueTask<HttpClient> GetHttpClient()
    {
        return ValueTask.FromResult(httpClientFactory.CreateClient(FwHeadlessKernel.LexboxHttpClientName));
    }

    public ValueTask<ConnectionStatus> ConnectionStatus(bool forceRefresh = false)
    {
        return new ValueTask<ConnectionStatus>(MiniLcm.Project.ConnectionStatus.Online);
    }
}
