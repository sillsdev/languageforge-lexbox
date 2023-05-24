using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Shouldly;

namespace Testing.SyncReverseProxy;

public class GetProjectDataTests
{
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

    [Theory]
    // [InlineData("https://admin.languageforge.org")]
    [InlineData("https://localhost:7075")]
    public async Task GetProjectData(string host)
    {
        var response = await Client.PostAsync($"{host}/api/user/{TestData.User}/projects",
            new FormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string, string>("password", TestData.Password)
                }));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.ShouldBe(JsonValueKind.Array);
        var projectArray = JsonArray.Create(content);
        projectArray.ShouldNotBeNull();
        projectArray.Count.ShouldBe(1);
        var project = projectArray.ShouldHaveSingleItem() as JsonObject;
        project.ShouldNotBeNull();
        var projectDict = new Dictionary<string, JsonNode?>(project);
        projectDict.ShouldSatisfyAllConditions(
            () => projectDict.ShouldContainKey("identifier"),
            () => projectDict.ShouldContainKey("name"),
            () => projectDict.ShouldContainKey("repository"),
            () => projectDict.ShouldContainKey("role")
        );
        project["identifier"]!.GetValue<string>().ShouldBe(TestData.ProjectCode);
        project["name"]!.GetValue<string>().ShouldBe("Sena 3");
        project["repository"]!.GetValue<string>().ShouldBe("http://public.languagedepot.org");
        //todo what is role for? returns unknown in my single test
        project["role"]!.GetValue<string>().ShouldNotBeEmpty();
    }

    [Theory]
    // [InlineData("https://admin.languageforge.org")]
    [InlineData("https://localhost:7075")]
    public async Task TestInvalidPassword(string host)
    {
        var response = await Client.PostAsync($"{host}/api/user/{TestData.User}/projects",
            new FormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string, string>("password", "bad password")
                }));
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.ShouldBe(JsonValueKind.Object);
        var responseObject = JsonObject.Create(content);
        responseObject.ShouldNotBeNull();
        responseObject.ShouldContainKey("error");
        responseObject["error"]!.GetValue<string>().ShouldBe("Bad password");
    }

    [Theory]
    // [InlineData("https://admin.languageforge.org")]
    [InlineData("https://localhost:7075")]
    public async Task TestInvalidUser(string host)
    {
        var response = await Client.PostAsync($"{host}/api/user/not-a-real-user-account/projects",
            new FormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string, string>("password", "doesn't matter")
                }));
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.ShouldBe(JsonValueKind.Object);
        var responseObject = JsonObject.Create(content);
        responseObject.ShouldNotBeNull();
        responseObject.ShouldContainKey("error");
        responseObject["error"]!.GetValue<string>().ShouldBe("Unknown user");
    }
}