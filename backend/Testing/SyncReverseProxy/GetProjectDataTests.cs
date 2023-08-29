using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Shouldly;
using Testing.Services;

namespace Testing.SyncReverseProxy;

[Trait("Category", "Integration")]
public class GetProjectDataTests
{
    private readonly string _baseUrl = TestingEnvironmentVariables.ServerBaseUrl;
    private static readonly HttpClient Client = new();

    private const string SampleRequest = """
POST https://admin.languageforge.org/api/user/{userName}/projects HTTP/1.1
Content-Type: application/x-www-form-urlencoded
Host: admin.languageforge.org
Content-Length: 24
Expect: 100-continue
Connection: Keep-Alive

password={password}
""";

    private async Task ValidateResponse(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.ShouldBe(JsonValueKind.Array);
        var projectArray = JsonArray.Create(content);
        projectArray.ShouldNotBeNull();
        projectArray.Count.ShouldBeGreaterThan(0);
        var project =
            projectArray.First(p => p?["identifier"]?.GetValue<string>() == TestingEnvironmentVariables.ProjectCode) as JsonObject;
        project.ShouldNotBeNull();
        var projectDict = new Dictionary<string, JsonNode?>(project);
        projectDict.ShouldSatisfyAllConditions(
            () => projectDict.ShouldContainKey("identifier"),
            () => projectDict.ShouldContainKey("name"),
            () => projectDict.ShouldContainKey("repository"),
            () => projectDict.ShouldContainKey("role")
        );
        project["identifier"]!.GetValue<string>().ShouldBe(TestingEnvironmentVariables.ProjectCode);
        project["name"]!.GetValue<string>().ShouldBe("Sena 3");
        project["repository"]!.GetValue<string>().ShouldBe("http://public.languagedepot.org");
        //todo what is role for? returns unknown in my single test
        project["role"]!.GetValue<string>().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetProjectDataViaForm()
    {
        var response = await Client.PostAsync(
            $"{_baseUrl}/api/user/{TestData.User}/projects",
            new FormUrlEncodedContent(
                new[] { new KeyValuePair<string, string>("password", TestData.Password) }));
        await ValidateResponse(response);
    }

    [Fact]
    public async Task GetProjectDataViaJson()
    {
        var response = await Client.PostAsJsonAsync(
            $"{_baseUrl}/api/user/{TestData.User}/projects",
            new { password = TestData.Password });
        await ValidateResponse(response);
    }

    [Fact]
    public async Task TestInvalidPassword()
    {
        var response = await Client.PostAsync(
            $"{_baseUrl}/api/user/{TestData.User}/projects",
            new FormUrlEncodedContent(
                new[] { new KeyValuePair<string, string>("password", "bad password") }));
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.ShouldBe(JsonValueKind.Object);
        var responseObject = JsonObject.Create(content);
        responseObject.ShouldNotBeNull();
        responseObject.ShouldContainKey("error");
        responseObject["error"]!.GetValue<string>().ShouldBe("Bad password");
    }

    [Fact]
    public async Task TestInvalidUser()
    {
        var response = await Client.PostAsync(
            $"{_baseUrl}/api/user/not-a-real-user-account/projects",
            new FormUrlEncodedContent(
                new[] { new KeyValuePair<string, string>("password", "doesn't matter") }));
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.ShouldBe(JsonValueKind.Object);
        var responseObject = JsonObject.Create(content);
        responseObject.ShouldNotBeNull();
        responseObject.ShouldContainKey("error");
        responseObject["error"]!.GetValue<string>().ShouldBe("Unknown user");
    }
}
