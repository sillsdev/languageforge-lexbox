using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using LexCore.Auth;
using Shouldly;
using Testing.Services;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
public class FlexJwtTests : ApiTestBase
{
    private static readonly JwtSecurityTokenHandler TokenHandler = new();

    private async Task<string> GetFlexJwt()
    {
        var userJwt = await JwtHelper.GetJwtForUser(new SendReceiveAuth("manager",
            TestingEnvironmentVariables.DefaultPassword));
        //todo use the open with flex url to retrieve a flex jwt
        return userJwt;
    }

    private LexAuthUser ParseUserToken(string jwt)
    {
        var outputJwt = TokenHandler.ReadJwtToken(jwt);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(outputJwt.Claims, "Testing"));
        return LexAuthUser.FromClaimsPrincipal(principal) ?? throw new NullReferenceException("User was null");
    }

    [Fact]
    public async Task CanGetProjectSpecificToken()
    {
        var flexJwt = await GetFlexJwt();
        var response = await HttpClient.SendAsync(new (HttpMethod.Get,
            $"{BaseUrl}/api/integration/getProjectToken?projectCode={TestingEnvironmentVariables.ProjectCode}")
        {
            Headers = { Authorization = new("Bearer", flexJwt) }
        });
        response.EnsureSuccessStatusCode();
        //intentionally not using the RefreshResponse class to make sure this test still fails if properties are renamed
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var projectToken = json.GetProperty("projectToken").GetString();
        projectToken.ShouldNotBeEmpty();
        var user = ParseUserToken(projectToken);
        user.Projects.ShouldHaveSingleItem();
        user.Audience.ShouldBe(LexboxAudience.SendAndReceive);

        var flexToken = json.GetProperty("flexToken").GetString();
        flexToken.ShouldNotBeEmpty();
        var flexUser = ParseUserToken(flexToken);
        flexUser.Projects.ShouldBeEmpty();
        flexUser.Audience.ShouldBe(LexboxAudience.SendAndReceiveRefresh);

        json.GetProperty("projectTokenExpiresAt").GetDateTime().ShouldNotBe(default);
        json.GetProperty("flexTokenExpiresAt").GetDateTime().ShouldNotBe(default);
    }
}
