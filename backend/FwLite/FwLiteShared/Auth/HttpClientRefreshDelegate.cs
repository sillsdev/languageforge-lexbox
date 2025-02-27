using LexCore.Auth;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.Auth;

public class HttpClientRefreshDelegate(OAuthClientFactory clientFactory, ILogger<HttpClientRefreshDelegate> logger) : DelegatingHandler
{
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = base.Send(request, cancellationToken);
        HandleResponse(response);
        return response;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        HandleResponse(response);
        return response;
    }

    private void HandleResponse(HttpResponseMessage response)
    {
        var authority = response.RequestMessage?.RequestUri?.Authority;
        if (string.IsNullOrEmpty(authority))
        {
            logger.LogWarning("Unable to get authority from response");
            return;
        }

        if (!response.Headers.TryGetValues(LexAuthConstants.JwtUpdatedHeader, out var values))
        {
            return;
        }
        if (!values.Any()) return;

        _ = Task.Run(() => clientFactory.GetClient(authority).RefreshToken());

    }
}
