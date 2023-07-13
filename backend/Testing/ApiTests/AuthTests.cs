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
    private string _host = TestingEnvironmentVariables.ServerHostname;
    private HttpClient _httpClient = new HttpClient();

    private async Task LoginAs(string user, string password)
    {
        await _httpClient.PostAsJsonAsync(
            $"http://{_host}/api/login",
            new Dictionary<string, object>
            {
                { "password", password }, { "emailOrUsername", user }, { "preHashedPassword", false }
            });
    }

    [Fact]
    public async Task TestLoginAndVerifyDifferentUsers()
    {
        await LoginAs("manager", "pass");
        var managerResponse = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"http://{_host}/api/user/currentUser"),
            HttpCompletionOption.ResponseContentRead);
        var manager = await managerResponse.Content.ReadFromJsonAsync<LexAuthUser>();
        manager.ShouldNotBeNull();
        manager.Email.ShouldBe("manager@test.com");

        await LoginAs("admin", "pass");
        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"http://{_host}/api/user/currentUser"),
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
                $"http://{_host}/api/graphql")
            {
                Content = new StringContent(query, Encoding.UTF8, "application/json")
            },
            HttpCompletionOption.ResponseContentRead);
        var manager = await managerResponse.Content.ReadFromJsonAsync<JsonObject>();
        manager.ShouldNotBeNull();
        manager["data"]?["me"]?["email"]?.ToString().ShouldBe("manager@test.com");

        await LoginAs("admin", "pass");
        var adminResponse = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post,
                $"http://{_host}/api/graphql")
            {
                Content = new StringContent(query, Encoding.UTF8, "application/json")
            },
            HttpCompletionOption.ResponseContentRead);
        var admin = await adminResponse.Content.ReadFromJsonAsync<JsonObject>();
        admin.ShouldNotBeNull();
        admin["data"]?["me"]?["email"]?.ToString().ShouldBe("admin@test.com");
    }
}
