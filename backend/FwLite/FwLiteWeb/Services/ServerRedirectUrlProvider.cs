using FwLiteShared.Auth;
using FwLiteWeb.Routes;

namespace FwLiteWeb.Services;

public class ServerRedirectUrlProvider(LinkGenerator linkGenerator, UrlContext urlContext): IRedirectUrlProvider
{
    public string? GetRedirectUrl()
    {
        var (hostUrl, _) = urlContext.GetUrl();
        return RedirectUrlFromHost(hostUrl);
    }

    private string? RedirectUrlFromHost(Uri hostUrl)
    {
        var redirectHost = HostString.FromUriComponent(hostUrl);
        return linkGenerator.GetUriByRouteValues(AuthRoutes.CallbackRoute,
            new RouteValueDictionary(),
            hostUrl.Scheme,
            redirectHost);
    }

    public bool ShouldRecreateAuthHelper(string? redirectUrl)
    {
        var (hostUrl, guess) = urlContext.GetUrl();
        if (guess) return false;
        return RedirectUrlFromHost(hostUrl) != redirectUrl;
    }
}
