using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LexCore.Auth;
using Shouldly;
using Testing.Services;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
public class AuthTests
{
    private HttpClient _httpClient = new HttpClient();

    private async Task<string> LoginAs(string user, string password)
    {
        var cookieContainer = new CookieContainer();
        var loginResponse = await _httpClient.PostAsJsonAsync(
            $"http://{TestingEnvironmentVariables.ServerHostname}/api/login",
            new Dictionary<string, object>
            {
                { "password", password }, { "emailOrUsername", user }, { "preHashedPassword", false }
            });
        foreach (var value in loginResponse.Headers.GetValues("Set-Cookie"))
        {
            cookieContainer.SetCookies(loginResponse.RequestMessage?.RequestUri ??
                                       throw new ArgumentNullException("requestUri"),
                value);
        }

        var lexBoxCookie = cookieContainer.GetAllCookies().FirstOrDefault(c => c.Name == ".LexBoxAuth");
        lexBoxCookie.ShouldNotBeNull();
        return lexBoxCookie.Value;
    }

    [Fact]
    public async Task TestLoginAndVerifyDifferentUsers()
    {
        await LoginAs("manager", "pass");
        var managerResponse = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"http://{TestingEnvironmentVariables.ServerHostname}/api/user/currentUser"),
            HttpCompletionOption.ResponseContentRead);
        var manager = await managerResponse.Content.ReadFromJsonAsync<LexAuthUser>();
        manager.ShouldNotBeNull();
        manager.Email.ShouldBe("manager@test.com");

        await LoginAs("admin", "pass");
        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"http://{TestingEnvironmentVariables.ServerHostname}/api/user/currentUser"),
            HttpCompletionOption.ResponseContentRead);
        var admin = await response.Content.ReadFromJsonAsync<LexAuthUser>();
        admin.ShouldNotBeNull();
        admin.Email.ShouldBe("admin@test.com");
    }

    [Fact]
    public async Task TestGqlVerifyDifferentUsers()
    {
        var query = """{"query":"query testGetMe {  me {    id    email  }}"}""";
        await LoginAs("manager", "pass");
        var managerResponse = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post,
                $"http://{TestingEnvironmentVariables.ServerHostname}/api/graphql")
            {
                Content = new StringContent(query, Encoding.UTF8, "application/json")
            },
            HttpCompletionOption.ResponseContentRead);
        var manager = await managerResponse.Content.ReadFromJsonAsync<JsonObject>();
        manager.ShouldNotBeNull();
        manager["data"]?["me"]?["email"]?.ToString().ShouldBe("manager@test.com");

        await LoginAs("admin", "pass");
        var adminResponse = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post,
                $"http://{TestingEnvironmentVariables.ServerHostname}/api/graphql")
            {
                Content = new StringContent(query, Encoding.UTF8, "application/json")
            },
            HttpCompletionOption.ResponseContentRead);
        var admin = await adminResponse.Content.ReadFromJsonAsync<JsonObject>();
        admin.ShouldNotBeNull();
        admin["data"]?["me"]?["email"]?.ToString().ShouldBe("admin@test.com");
    }
}
