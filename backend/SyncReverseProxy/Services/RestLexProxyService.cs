using System.Net;
using System.Security.Claims;
using LexCore;
using LexCore.Auth;
using LexCore.ServiceInterfaces;
using LexSyncReverseProxy.Config;
using Microsoft.Extensions.Options;

namespace LexSyncReverseProxy.Services;

public class RestLexProxyService : ILexProxyService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly LexBoxApiConfig _lexBoxApiConfig;

    public RestLexProxyService(IHttpClientFactory clientFactory, IOptionsSnapshot<LexBoxApiConfig> options)
    {
        _clientFactory = clientFactory;
        _lexBoxApiConfig = options.Value;
    }

    private HttpClient GetClient()
    {
        var client = _clientFactory.CreateClient("admin");
        client.BaseAddress = new Uri(_lexBoxApiConfig.Url);
        return client;
    }

    public async Task<ClaimsPrincipal?> Login(LoginRequest loginRequest)
    {
        var client = GetClient();
        var response = await client.PostAsJsonAsync("/api/login", loginRequest);
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
            return null;
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<LexAuthUser>();

        return user?.GetPrincipal("LexApi");
    }

    public async Task RefreshProjectLastChange(string projectCode)
    {
        var client = GetClient();
        //we don't care about the response, we just want it to update
        await client.PostAsync($"/api/project/refreshProjectLastChanged?projectCode={projectCode}", null);
    }
}