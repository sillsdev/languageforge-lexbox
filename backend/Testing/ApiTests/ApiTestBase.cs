using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using LexCore.Auth;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Testing.Fixtures;
using Testing.LexCore.Utils;
using Testing.Services;

namespace Testing.ApiTests;

public class ApiTestBase
{
    public string BaseUrl => TestingEnvironmentVariables.ServerBaseUrl;
    private readonly SocketsHttpHandler _httpClientHandler;
    public readonly HttpClient HttpClient;
    /// <summary>
    /// Shares cookies (and so the login) with <see cref="HttpClient"/> but never retries.
    /// Use it for requests whose body can't be replayed (uploads) or where the exact response matters.
    /// </summary>
    public readonly HttpClient HttpClientWithoutRetry;
    public string? CurrJwt { get; private set; }
    public LexAuthUser CurrentUser => JwtHelper.ToLexAuthUser(CurrJwt!);

    public ApiTestBase()
    {
        (_httpClientHandler, HttpClient) = NewHttpClient(BaseUrl);
        HttpClientWithoutRetry = WithBaseUrl(new HttpClient(_httpClientHandler, disposeHandler: false), BaseUrl);
    }

    /// <summary>
    /// creates an HttpClient which will retry on transient failures (unless retry is false)
    /// </summary>
    /// <param name="baseUrl">bas url for the client</param>
    /// <param name="useCookies">enable or disable cookies for the client</param>
    /// <param name="retry">retry transient failures (incl. 5xx responses), disable to observe error responses exactly as returned</param>
    public static (SocketsHttpHandler Handler, HttpClient Client) NewHttpClient(string? baseUrl = null, bool useCookies = true, bool retry = true)
    {
        var socketsHttpHandler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15), UseCookies = useCookies };
        if (!retry) return (socketsHttpHandler, WithBaseUrl(new HttpClient(socketsHttpHandler), baseUrl));

        var retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions { BackoffType = DelayBackoffType.Linear, MaxRetryAttempts = 3 })
            .Build();

#pragma warning disable EXTEXP0001
        var resilienceHandler = new ResilienceHandler(retryPipeline) { InnerHandler = socketsHttpHandler };
#pragma warning restore EXTEXP0001
        return (socketsHttpHandler, WithBaseUrl(new HttpClient(resilienceHandler), baseUrl));
    }

    private static HttpClient WithBaseUrl(HttpClient httpClient, string? baseUrl)
    {
        if (!string.IsNullOrEmpty(baseUrl))
        {
            httpClient.BaseAddress = new Uri(baseUrl);
        }
        return httpClient;
    }

    // This needs to be virtual so it can be mocked in IntegrationFixtureTests
    public virtual async Task<string> LoginAs(string user, string? password = null, bool includeDefaultScope = true)
    {
        password ??= TestingEnvironmentVariables.DefaultPassword;
        var response = await JwtHelper.ExecuteLogin(new SendReceiveAuth(user, password), includeDefaultScope, HttpClient);
        CurrJwt = JwtHelper.GetJwtFromLoginResponse(response);
        return CurrJwt;
    }

    public void ClearCookies()
    {
        JwtHelper.ClearCookies(_httpClientHandler);
        // the no-retry helpers treat CurrJwt as "logged in", and that's no longer true without the cookie
        CurrJwt = null;
    }

    public async Task<JsonObject> ExecuteGql([StringSyntax("graphql")] string gql, bool expectGqlError = false, bool expectSuccessCode = true,
        string? overrideJwt = null)
    {
        var jwtParam = overrideJwt is not null ? $"?jwt={overrideJwt}" : "";
        var response = await HttpClient.PostAsJsonAsync($"{BaseUrl}/api/graphql{jwtParam}", new { query = gql });
        if (expectSuccessCode) response.ShouldBeSuccessful();
        if (JwtHelper.TryGetJwtFromLoginResponse(response, out var jwt)) CurrJwt = jwt;
        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
        jsonResponse.Should().NotBeNull($"for query {gql} ({(int)response.StatusCode} ({response.ReasonPhrase}))");
        GqlUtils.ValidateGqlErrors(jsonResponse, expectGqlError);
        return jsonResponse;
    }

    public async Task<DateTimeOffset?> GetProjectLastCommit(string projectCode)
    {
        var state = await this.GetProjectRepoState(projectCode);
        return state.LastCommit;
    }

    public async Task StartLexboxProjectReset(string projectCode)
    {
        var response = await HttpClient.PostAsync($"{BaseUrl}/api/project/resetProject/{projectCode}", null);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Finishes a reset without uploading a zip, leaving the project with an empty repo
    /// </summary>
    public async Task FinishLexboxProjectResetWithoutUpload(string projectCode)
    {
        var response = await HttpClient.PostAsync($"{BaseUrl}/api/project/finishResetProject/{projectCode}", null);
        response.ShouldBeSuccessful();
    }

    public async Task<string> GetSendAndReceiveJwt()
    {
        CurrJwt.Should().NotBeNullOrEmpty("we should be logged in already");
        var response = await HttpClient.SendAsync(new(HttpMethod.Get,
            $"{BaseUrl}/api/integration/getProjectToken?projectCode={TestingEnvironmentVariables.ProjectCode}")
        {
            Headers = { Authorization = new("Bearer", CurrJwt) }
        });
        response.EnsureSuccessStatusCode();
        //intentionally not using the RefreshResponse class to make sure this test still fails if properties are renamed
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var projectToken = json.GetProperty("projectToken").GetString();
        projectToken.Should().NotBeNullOrEmpty();
        return projectToken;
    }
}
