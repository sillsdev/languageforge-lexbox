using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using LexCore.Auth;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Shouldly;
using Testing.LexCore.Utils;
using Testing.Services;

namespace Testing.ApiTests;

public class ApiTestBase
{
    public string BaseUrl => TestingEnvironmentVariables.ServerBaseUrl;
    private readonly SocketsHttpHandler _httpClientHandler;
    public readonly HttpClient HttpClient;
    public string? CurrJwt { get; private set; }
    public LexAuthUser CurrentUser => JwtHelper.ToLexAuthUser(CurrJwt!);

    public ApiTestBase()
    {
        (_httpClientHandler, HttpClient) = NewHttpClient(BaseUrl);
    }

    /// <summary>
    /// creates an HttpClient which will retry on transient failures
    /// </summary>
    /// <param name="baseUrl">bas url for the client</param>
    /// <param name="useCookies">enable or disable cookies for the client</param>
    public static (SocketsHttpHandler Handler, HttpClient Client) NewHttpClient(string? baseUrl = null, bool useCookies = true)
    {
        var retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions { BackoffType = DelayBackoffType.Linear, MaxRetryAttempts = 3 })
            .Build();

        var socketsHttpHandler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15), UseCookies = useCookies };
#pragma warning disable EXTEXP0001
        var resilienceHandler = new ResilienceHandler(retryPipeline) { InnerHandler = socketsHttpHandler };
#pragma warning restore EXTEXP0001
        var httpClient = new HttpClient(resilienceHandler);
        if (!string.IsNullOrEmpty(baseUrl))
        {
            httpClient.BaseAddress = new Uri(baseUrl);
        }
        return (socketsHttpHandler, httpClient);
    }

    // This needs to be virtual so it can be mocked in IntegrationFixtureTests
    public virtual async Task<string> LoginAs(string user, string? password = null)
    {
        password ??= TestingEnvironmentVariables.DefaultPassword;
        var response = await JwtHelper.ExecuteLogin(new SendReceiveAuth(user, password), HttpClient);
        CurrJwt = JwtHelper.GetJwtFromLoginResponse(response);
        return CurrJwt;
    }

    public void ClearCookies()
    {
        JwtHelper.ClearCookies(_httpClientHandler);
    }

    public async Task<JsonObject> ExecuteGql([StringSyntax("graphql")] string gql, bool expectGqlError = false, bool expectSuccessCode = true,
        string? overrideJwt = null)
    {
        var jwtParam = overrideJwt is not null ? $"?jwt={overrideJwt}" : "";
        var response = await HttpClient.PostAsJsonAsync($"{BaseUrl}/api/graphql{jwtParam}", new { query = gql });
        if (JwtHelper.TryGetJwtFromLoginResponse(response, out var jwt)) CurrJwt = jwt;
        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
        jsonResponse.ShouldNotBeNull($"for query {gql} ({(int)response.StatusCode} ({response.ReasonPhrase}))");
        GqlUtils.ValidateGqlErrors(jsonResponse, expectGqlError);
        if (expectSuccessCode)
            response.IsSuccessStatusCode.ShouldBeTrue($"code was {(int)response.StatusCode} ({response.ReasonPhrase})");
        return jsonResponse;
    }

    public async Task<string?> GetProjectLastCommit(string projectCode)
    {
        var jsonResult = await ExecuteGql($$"""
query projectLastCommit {
    projectByCode(code: "{{projectCode}}") {
        lastCommit
    }
}
""");
        var project = jsonResult?["data"]?["projectByCode"].ShouldBeOfType<JsonObject>();
        return project?["lastCommit"]?.ToString();
    }

    public async Task StartLexboxProjectReset(string projectCode)
    {
        var response = await HttpClient.PostAsync($"{BaseUrl}/api/project/resetProject/{projectCode}", null);
        response.EnsureSuccessStatusCode();
    }
}
