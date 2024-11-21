using System.Net.Http.Json;
using System.Text.Json;
using LexCore.Auth;
using FluentAssertions;
using Testing.Services;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
public class FlexJwtTests : ApiTestBase
{
    private async Task<string> GetFlexJwt()
    {
        var userJwt = await JwtHelper.GetJwtForUser(new SendReceiveAuth("manager",
            TestingEnvironmentVariables.DefaultPassword));
        //todo use the open with flex url to retrieve a flex jwt
        return userJwt;
    }

    private LexAuthUser ParseUserToken(string jwt)
    {
        return JwtHelper.ToLexAuthUser(jwt);
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
        projectToken.ShouldNotBeNullOrEmpty();
        var user = ParseUserToken(projectToken);
        user.Projects.Should().ContainSingle();
        user.Audience.Should().Be(LexboxAudience.SendAndReceive);

        var flexToken = json.GetProperty("flexToken").GetString();
        flexToken.ShouldNotBeNullOrEmpty();
        var flexUser = ParseUserToken(flexToken);
        flexUser.Projects.Should().BeEmpty();
        flexUser.Audience.Should().Be(LexboxAudience.SendAndReceiveRefresh);

        json.GetProperty("projectTokenExpiresAt").GetDateTime().Should().NotBe(default);
        json.GetProperty("flexTokenExpiresAt").GetDateTime().Should().NotBe(default);
    }
}
