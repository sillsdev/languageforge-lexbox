using MiniLcm.Project;

namespace FwHeadless.Services;

public class LexboxServerHttpClientProvider(IHttpClientFactory httpClientFactory) : IServerHttpClientProvider
{
    public ValueTask<HttpClient> GetHttpClient()
    {
        return ValueTask.FromResult(httpClientFactory.CreateClient(FwHeadlessKernel.LexboxHttpClientName));
    }
}
