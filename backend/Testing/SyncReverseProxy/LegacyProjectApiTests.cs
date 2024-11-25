using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using FluentAssertions.Execution;
using Testing.ApiTests;
using Testing.Services;

namespace Testing.SyncReverseProxy;

[Trait("Category", "Integration")]
public class LegacyProjectApiTests
{
    private readonly string _baseUrl = TestingEnvironmentVariables.ServerBaseUrl;
    private static readonly HttpClient Client = ApiTestBase.NewHttpClient().Client;

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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.Should().Be(JsonValueKind.Array);
        var projectArray = JsonArray.Create(content);
        projectArray.Should().NotBeNull();
        projectArray.Count.Should().BeGreaterThan(0);
        var project =
            projectArray.First(p => p?["identifier"]?.GetValue<string>() == TestingEnvironmentVariables.ProjectCode) as JsonObject;
        project.Should().NotBeNull();
        var projectDict = new Dictionary<string, JsonNode?>(project);
        using (new AssertionScope())
        {
            projectDict.Should().ContainKey("identifier");
            projectDict.Should().ContainKey("name");
            projectDict.Should().ContainKey("repository");
            projectDict.Should().ContainKey("role");
        }
        project["identifier"]!.GetValue<string>().Should().Be(TestingEnvironmentVariables.ProjectCode);
        project["name"]!.GetValue<string>().Should().Be("Sena 3");
        project["repository"]!.GetValue<string>().Should().Be("http://public.languagedepot.org");
        //todo what is role for? returns unknown in my single test
        project["role"]!.GetValue<string>().Should().NotBeEmpty();
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
    public async Task GetProjectDataWithEmailAddress()
    {
        var response = await Client.PostAsync(
            $"{_baseUrl}/api/user/{TestData.User}@test.com/projects",
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
    public async Task GetProjectDataWithUppercaseUsername()
    {
        var response = await Client.PostAsJsonAsync(
            $"{_baseUrl}/api/user/{TestData.User.ToUpper()}/projects",
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
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.Should().Be(JsonValueKind.Object);
        var responseObject = JsonObject.Create(content);
        responseObject.Should().NotBeNull();
        responseObject.Should().ContainKey("error");
        responseObject["error"]!.GetValue<string>().Should().Be("Bad password");
    }

    [Fact]
    public async Task TestInvalidUser()
    {
        var response = await Client.PostAsync(
            $"{_baseUrl}/api/user/not-a-real-user-account/projects",
            new FormUrlEncodedContent(
                new[] { new KeyValuePair<string, string>("password", "doesn't matter") }));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.Should().Be(JsonValueKind.Object);
        var responseObject = JsonObject.Create(content);
        responseObject.Should().NotBeNull();
        responseObject.Should().ContainKey("error");
        responseObject["error"]!.GetValue<string>().Should().Be("Unknown user");
    }

    // LF sends lots of requests with no password/request body. Chorus might as well.
    // Requests between our software shouldn't be "Bad requests" (400).
    [Fact]
    public async Task MissingPasswordReturns403()
    {
        var response = await Client.PostAsJsonAsync<object?>($"{_baseUrl}/api/user/{TestData.User}/projects", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
