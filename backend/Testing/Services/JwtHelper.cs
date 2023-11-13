using System.Net;
using System.Net.Http.Json;
using LexBoxApi.Auth;
using Shouldly;

namespace Testing.Services;

public class JwtHelper
{
    private static readonly HttpClientHandler Handler = new();
    private static readonly HttpClient Client = new(Handler);

    public static async Task<string> GetJwtForUser(SendReceiveAuth auth)
    {
        var response = await ExecuteLogin(auth, Client);
        var jwt = GetJwtFromLoginResponse(response);
        ClearCookies(Handler);
        return jwt;
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

    public static void ClearCookies(HttpClientHandler httpClientHandler)
    {
        foreach (Cookie cookie in httpClientHandler.CookieContainer.GetAllCookies())
        {
            cookie.Expired = true;
        }
    }
}
