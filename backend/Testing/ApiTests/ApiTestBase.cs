using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Shouldly;
using Testing.Services;

namespace Testing.ApiTests;

public class ApiTestBase
{
    protected readonly string Host = TestingEnvironmentVariables.ServerHostname;
    protected readonly HttpClient HttpClient = new HttpClient();

    protected async Task LoginAs(string user, string password)
    {
        await HttpClient.PostAsJsonAsync(
            $"http://{Host}/api/login",
            new Dictionary<string, object>
            {
                { "password", password }, { "emailOrUsername", user }, { "preHashedPassword", false }
            });
    }

    protected async Task<JsonObject> ExecuteGql([StringSyntax("graphql")] string gql)
    {
        var response = await HttpClient.PostAsJsonAsync($"http://{Host}/api/graphql", new { query = gql });
        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
        response.IsSuccessStatusCode.ShouldBeTrue($"code was {(int)response.StatusCode} ({response.ReasonPhrase}), response was: {jsonResponse}");
        jsonResponse.ShouldNotBeNull("for query " + gql);
        return jsonResponse;
    }
}
