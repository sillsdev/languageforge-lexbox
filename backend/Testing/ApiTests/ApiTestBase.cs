using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Shouldly;
using Testing.LexCore.Utils;
using Testing.Services;

namespace Testing.ApiTests;

public class ApiTestBase
{
    public string BaseUrl => TestingEnvironmentVariables.ServerBaseUrl;
    private readonly HttpClientHandler _httpClientHandler = new();
    public readonly HttpClient HttpClient;

    public ApiTestBase()
    {
        HttpClient = new HttpClient(_httpClientHandler)
        {
            BaseAddress = new Uri(BaseUrl)
        };
    }

    public async Task<string> LoginAs(string user, string password)
    {
        var response = await JwtHelper.ExecuteLogin(new SendReceiveAuth(user, password), HttpClient);
        return JwtHelper.GetJwtFromLoginResponse(response);
    }

    public void ClearCookies()
    {
        JwtHelper.ClearCookies(_httpClientHandler);
    }

    public async Task<JsonObject> ExecuteGql([StringSyntax("graphql")] string gql, bool expectGqlError = false)
    {
        var response = await HttpClient.PostAsJsonAsync($"{BaseUrl}/api/graphql", new { query = gql });
        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
        jsonResponse.ShouldNotBeNull($"for query {gql} ({(int)response.StatusCode} ({response.ReasonPhrase}))");
        GqlUtils.ValidateGqlErrors(jsonResponse, expectGqlError);
        response.IsSuccessStatusCode.ShouldBeTrue($"code was {(int)response.StatusCode} ({response.ReasonPhrase})");
        return jsonResponse;
    }
}
