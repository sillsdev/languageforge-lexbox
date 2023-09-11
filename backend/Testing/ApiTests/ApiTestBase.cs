using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Shouldly;
using Testing.Services;

namespace Testing.ApiTests;

public class ApiTestBase
{
    public readonly string BaseUrl = TestingEnvironmentVariables.ServerBaseUrl;
    public readonly HttpClient HttpClient = new HttpClient();

    public async Task LoginAs(string user, string password)
    {
        var response = await HttpClient.PostAsJsonAsync(
            $"{BaseUrl}/api/login",
            new Dictionary<string, object>
            {
                { "password", password }, { "emailOrUsername", user }, { "preHashedPassword", false }
            });
        response.EnsureSuccessStatusCode();
    }

    public async Task<JsonObject> ExecuteGql([StringSyntax("graphql")] string gql, bool expectGqlError = false)
    {
        var response = await HttpClient.PostAsJsonAsync($"{BaseUrl}/api/graphql", new { query = gql });
        response.IsSuccessStatusCode.ShouldBeTrue($"code was {(int)response.StatusCode} ({response.ReasonPhrase})");
        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
        jsonResponse.ShouldNotBeNull("for query " + gql);
        if (!expectGqlError)
        {
            jsonResponse["errors"].ShouldBeNull();
        }
        return jsonResponse;
    }
}
