using System.Net.Http.Json;
using LexBoxApi.Auth;
using Shouldly;

namespace Testing.Services;

public class JwtHelper
{

    public static async Task<string> GetJwtForUser(SendReceiveAuth auth)
    {
        var handler = new HttpClientHandler();
        var client = new HttpClient(handler);
        var response = await client.PostAsJsonAsync(
            $"{TestingEnvironmentVariables.StandardHgBaseUrl}/api/login",
            new Dictionary<string, object>
            {
                { "password",  auth.Password }, { "emailOrUsername", auth.Username }, { "preHashedPassword", false }
            });
        response.EnsureSuccessStatusCode();
        var cookieContainer = handler.CookieContainer;
        var authCookie = cookieContainer.GetAllCookies().FirstOrDefault(c => c.Name == AuthKernel.AuthCookieName);
        authCookie.ShouldNotBeNull();
        var jwt = authCookie.Value;
        jwt.ShouldNotBeNullOrEmpty();
        return jwt;
    }
}
