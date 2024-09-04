using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LexBoxApi.Auth;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Shouldly;
using Testing.ApiTests;

namespace Testing.Services;

public class JwtHelper
{
    private static readonly SocketsHttpHandler Handler;
    private static readonly HttpClient Client;
    static JwtHelper()
    {
        (Handler, Client) = ApiTestBase.NewHttpClient();
    }

    public static async Task<string> GetJwtForUser(SendReceiveAuth auth)
    {
        var response = await ExecuteLogin(auth, Client);
        var jwt = GetJwtFromLoginResponse(response);
        ClearCookies(Handler);
        return jwt;
    }

    public static async Task<string> GetProjectJwtForUser(SendReceiveAuth auth, string projectCode)
    {
        var flexJwt = await GetJwtForUser(auth);
        var response = await Client.SendAsync(new(HttpMethod.Get,
            $"{TestingEnvironmentVariables.ServerBaseUrl}/api/integration/getProjectToken?projectCode={projectCode}")
        {
            Headers = { Authorization = new("Bearer", flexJwt) }
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("projectToken").GetString() ?? throw new NullReferenceException("projectToken was null");
    }

    public static async Task<HttpResponseMessage> ExecuteLogin(SendReceiveAuth auth, HttpClient httpClient)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"{TestingEnvironmentVariables.ServerBaseUrl}/api/login",
            new Dictionary<string, object>
            {
                { "password", auth.Password }, { "emailOrUsername", auth.Username }, { "preHashedPassword", false }
            });
        response.EnsureSuccessStatusCode();
        return response;
    }

    public static string GetJwtFromLoginResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var cookies = response.Headers.GetValues("Set-Cookie");
        var cookieContainer = new CookieContainer();
        cookieContainer.SetCookies(response.RequestMessage!.RequestUri!, cookies.Single());
        var authCookie = cookieContainer.GetAllCookies()
            .FirstOrDefault(c => c.Name == AuthKernel.AuthCookieName);
        authCookie.ShouldNotBeNull();
        var jwt = authCookie.Value;
        jwt.ShouldNotBeNullOrEmpty();
        return jwt;
    }

    public static void ClearCookies(SocketsHttpHandler httpClientHandler)
    {
        foreach (Cookie cookie in httpClientHandler.CookieContainer.GetAllCookies())
        {
            cookie.Expired = true;
        }
    }
}
