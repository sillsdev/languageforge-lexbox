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
        var response = await Client.PostAsJsonAsync(
            $"{TestingEnvironmentVariables.StandardHgBaseUrl}/api/login",
            new Dictionary<string, object>
            {
                { "password",  auth.Password }, { "emailOrUsername", auth.Username }, { "preHashedPassword", false }
            });
        response.EnsureSuccessStatusCode();
        var authCookie = Handler.CookieContainer.GetAllCookies()
            .FirstOrDefault(c => c.Name == AuthKernel.AuthCookieName);
        authCookie.ShouldNotBeNull();
        var jwt = authCookie.Value;
        jwt.ShouldNotBeNullOrEmpty();
        Handler.CookieContainer = new(); // reset the cookies as we're using a shared client
        return jwt;
    }
}
