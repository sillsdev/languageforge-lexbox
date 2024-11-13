using System.Net;
using LexCore;
using LexCore.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FwHeadless;

public class HttpClientAuthHandler(IOptions<FwHeadlessConfig> config, IMemoryCache cache, ILogger<HttpClientAuthHandler> logger) : DelegatingHandler
{
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("use async apis");
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var lexboxUrl = new Uri(config.Value.LexboxUrl);
        if (request.RequestUri?.Authority != lexboxUrl.Authority)
        {
            return await base.SendAsync(request, cancellationToken);
        }
        try
        {
            await SetAuthHeader(request, cancellationToken, lexboxUrl);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Unable to set auth header", e);
        }
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task SetAuthHeader(HttpRequestMessage request, CancellationToken cancellationToken, Uri lexboxUrl)
    {
        var cookieContainer = new CookieContainer();
        cookieContainer.Add(new Cookie(LexAuthConstants.AuthCookieName, await GetToken(cancellationToken), null, lexboxUrl.Authority));
        request.Headers.Add("Cookie", cookieContainer.GetCookieHeader(lexboxUrl));
    }

    private async ValueTask<string> GetToken(CancellationToken cancellationToken)
    {
        try
        {
            return await cache.GetOrCreateAsync("LexboxAuthToken",
                async entry =>
                {
                    if (InnerHandler is null) throw new InvalidOperationException("InnerHandler is null");
                    logger.LogInformation("Getting auth token");
                    var client = new HttpClient(InnerHandler);
                    client.BaseAddress = new Uri(config.Value.LexboxUrl);
                    var response = await client.PostAsJsonAsync("/api/login",
                        new LoginRequest(config.Value.LexboxPassword, config.Value.LexboxUsername),
                        cancellationToken);
                    response.EnsureSuccessStatusCode();
                    var cookies = response.Headers.GetValues("Set-Cookie");
                    var cookieContainer = new CookieContainer();
                    cookieContainer.SetCookies(response.RequestMessage!.RequestUri!, cookies.Single());
                    var authCookie = cookieContainer.GetAllCookies()
                        .FirstOrDefault(c => c.Name == LexAuthConstants.AuthCookieName);
                    if (authCookie is null) throw new InvalidOperationException("Auth cookie not found");
                    entry.SetValue(authCookie.Value);
                    entry.AbsoluteExpiration = authCookie.Expires;
                    logger.LogInformation("Got auth token: {AuthToken}", authCookie.Value);
                    return authCookie.Value;
                }) ?? throw new NullReferenceException("unable to get the login token");
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Unable to get auth token", e);
        }
    }
}
