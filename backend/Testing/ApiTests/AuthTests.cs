﻿using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using LexCore.Auth;
using LexSyncReverseProxy;
using LfClassicData;
using FluentAssertions;
using Testing.Services;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
public class AuthTests : ApiTestBase
{
    [Fact]
    public async Task TestLoginAndVerifyDifferentUsers()
    {
        await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        var managerResponse = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"{BaseUrl}/api/user/currentUser"),
            HttpCompletionOption.ResponseContentRead);
        managerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var manager = await managerResponse.Content.ReadFromJsonAsync<JsonElement>();
        manager.GetProperty("email").GetString().Should().Be("manager@test.com");

        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        var response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"{BaseUrl}/api/user/currentUser"),
            HttpCompletionOption.ResponseContentRead);
        var admin = await response.Content.ReadFromJsonAsync<JsonElement>();
        admin.GetProperty("email").GetString().Should().Be("admin@test.com");
    }

    [Fact]
    public async Task TestGqlVerifyDifferentUsers()
    {
        //language=GraphQL
        var query = """query testGetMe {  meAuth {    id    email  }}""";
        await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        var manager = await ExecuteGql(query);
        manager.Should().NotBeNull();
        manager["data"]!["meAuth"]!["email"]!.ToString().Should().Be("manager@test.com");

        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        var admin = await ExecuteGql(query);
        admin.Should().NotBeNull();
        admin["data"]!["meAuth"]!["email"]!.ToString().Should().Be("admin@test.com");
    }

    [Fact]
    public async Task NotLoggedInIsNotPermittedToCallRequiresAuthApi()
    {
        var response = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requires-auth");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ManagerIsPermittedToCallRequiresAuthApi()
    {
        await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        var response = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requires-auth");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task LexboxApiScopeIsRequiredByDefault()
    {
        var jwt = await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword, false);
        var user = JwtHelper.ToLexAuthUser(jwt);
        user.Scopes.Should().BeEmpty();
        var response = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requires-auth");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ManagerIsForbiddenFromAdminApi()
    {
        await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        var response = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requires-admin");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminIsPermittedToCallAdminApi()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        var response = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requires-admin");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task DefaultLoginCanNotCallExclusiveScopeApi()
    {
        await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        var response = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requires-forgot-password");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        response = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requires-forgot-password");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DefaultJwtCanNotCallSendReceiveRequiredScope()
    {
        await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        var response = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requiresSendReceiveScope");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        response = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requiresSendReceiveScope");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SendReceiveRequiredJwtCanCallSendReceiveRequiredScope()
    {
        await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        var srJwt = await GetSendAndReceiveJwt();
        var response = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requiresSendReceiveScope?jwt={srJwt}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        srJwt = await GetSendAndReceiveJwt();
        response = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requiresSendReceiveScope?jwt={srJwt}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminRequiredAttributeDoesNotOverrideScopeRequirement()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        var srJwt = await GetSendAndReceiveJwt();
        var response = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requires-admin-and-sr-scope?jwt={srJwt}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseDefaultScope = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requires-admin-and-sr-scope");
        responseDefaultScope.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ClearingCookiesWorks()
    {
        await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        ClearCookies();
        var response = await HttpClient.GetAsync($"{BaseUrl}/api/AuthTesting/requires-auth");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = "Not working due to oauth, to solve we should setup a login via oauth to use the right jwt")]
    public async Task CanUseBearerAuth()
    {
        var jwt = await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        ClearCookies();
        var response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"{BaseUrl}/api/AuthTesting/requires-auth")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", jwt) }
        });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task JwtWithInvalidSignatureFailsAuth()
    {
        var jwt = await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        ClearCookies();

        //fiddle with jwt to try and elevate permissions
        var tokenHandler = new JwtSecurityTokenHandler();
        var originalJwtToken = tokenHandler.ReadJwtToken(jwt);
        var claims = originalJwtToken.Payload.Claims.ToArray();
        var roleClaimIndex = Array.FindIndex(claims, c => c.Type == LexAuthConstants.RoleClaimType);
        //change role to admin
        claims[roleClaimIndex] = new Claim(LexAuthConstants.RoleClaimType, UserRole.admin.ToString());
        var payloadEncoded = new JwtPayload(claims).Base64UrlEncode();
        var newJwt = $"{originalJwtToken.RawHeader}.{payloadEncoded}.{originalJwtToken.RawSignature}";

        var response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"{BaseUrl}/api/AuthTesting/requires-auth")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", newJwt) }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    //these must match because auth determines the project code from the route key using the method in HgHelpers
    //but I don't want to make the projects reference one or the other, so I'm just using a test
    [Fact]
    public void RouteKeyInLfClassicRoutesMustMatchRouteKeyInProxyConstants()
    {
        LfClassicRoutes.ProjectCodeRouteKey.Should().Be(ProxyConstants.HgProjectCodeRouteKey);
    }
}
