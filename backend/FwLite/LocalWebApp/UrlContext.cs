using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace LocalWebApp;

public class UrlContext(IServer server, IHttpContextAccessor contextAccessor)
{
    /// <summary>
    /// url returned is a guess when it comes from IServer, instead of IHttpContext
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public (Uri host, bool guess) GetUrl()
    {
        var httpContext = contextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var uriBuilder = new UriBuilder(httpContext.Request.Scheme, httpContext.Request.Host.Host.Replace("127.0.0.1", "localhost"), httpContext.Request.Host.Port ?? 80);
            return (uriBuilder.Uri, false);
        }
        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault() ?? throw new InvalidOperationException("No server address");
        if (address.StartsWith("http://127.0.0.1")) address = address.Replace("http://127.0.0.1", "http://localhost");
        if (address.StartsWith("https://127.0.0.1")) address = address.Replace("https://127.0.0.1", "http://localhost");
        return (new Uri(address), true);
    }
}
