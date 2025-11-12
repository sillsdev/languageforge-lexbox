using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using LexBoxApi.Auth;
using LexCore.Auth;
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
        var response = await ExecuteLogin(auth, true, Client);
        var jwt = await GetJwtFromLoginResponse(response);
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
        await EnsureSuccessStatusCodeWithBody(response);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("projectToken").GetString() ?? throw new NullReferenceException("projectToken was null");
    }

    public static async Task<HttpResponseMessage> ExecuteLogin(SendReceiveAuth auth,
        bool includeDefaultScope,
        HttpClient httpClient)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"{TestingEnvironmentVariables.ServerBaseUrl}/api/login?defaultScope={includeDefaultScope}",
            new Dictionary<string, object>
            {
                { "password", auth.Password }, { "emailOrUsername", auth.Username }, { "preHashedPassword", false }
            });
        await EnsureSuccessStatusCodeWithBody(response);
        return response;
    }

    public static async Task<string> GetJwtFromLoginResponse(HttpResponseMessage response)
    {
        await EnsureSuccessStatusCodeWithBody(response);
        TryGetJwtFromLoginResponse(response, out var jwt);
        jwt.Should().NotBeNullOrEmpty();
        return jwt;
    }

    public static bool TryGetJwtFromLoginResponse(HttpResponseMessage response, out string? jwt)
    {
        jwt = null;
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var cookieContainer = new CookieContainer();
            cookieContainer.SetCookies(response.RequestMessage!.RequestUri!, cookies.Single());
            var authCookie = cookieContainer.GetAllCookies()
                .FirstOrDefault(c => c.Name == AuthKernel.AuthCookieName);
            jwt = authCookie?.Value;
        }
        return jwt is not null;
    }

    /// <summary>
    /// Ensures the HTTP response has a success status code, including the response body in the exception message if it fails.
    /// This is more helpful than HttpResponseMessage.EnsureSuccessStatusCode() which doesn't include the body.
    /// </summary>
    public static async Task EnsureSuccessStatusCodeWithBody(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response body: {errorContent}");
        }
    }

    public static void ClearCookies(SocketsHttpHandler httpClientHandler)
    {
        foreach (Cookie cookie in httpClientHandler.CookieContainer.GetAllCookies())
        {
            cookie.Expired = true;
        }
    }

    private static readonly JwtSecurityTokenHandler TokenHandler = new();
    public static LexAuthUser ToLexAuthUser(string jwt)
    {
        var outputJwt = TokenHandler.ReadJwtToken(jwt);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(outputJwt.Claims, "Testing"));
        return LexAuthUser.FromClaimsPrincipal(principal) ?? throw new NullReferenceException("User was null");
    }
}
