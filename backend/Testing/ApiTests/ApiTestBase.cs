﻿using System.Diagnostics.CodeAnalysis;
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

    // This needs to be virtual so it can be mocked in IntegrationFixtureTests
    public virtual async Task<string> LoginAs(string user, string? password = null)
    {
        password ??= TestingEnvironmentVariables.DefaultPassword;
        var response = await JwtHelper.ExecuteLogin(new SendReceiveAuth(user, password), HttpClient);
        return JwtHelper.GetJwtFromLoginResponse(response);
    }

    public void ClearCookies()
    {
        JwtHelper.ClearCookies(_httpClientHandler);
    }

    public async Task<JsonObject> ExecuteGql([StringSyntax("graphql")] string gql, bool expectGqlError = false, bool expectSuccessCode = true)
    {
        var response = await HttpClient.PostAsJsonAsync($"{BaseUrl}/api/graphql", new { query = gql });
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
