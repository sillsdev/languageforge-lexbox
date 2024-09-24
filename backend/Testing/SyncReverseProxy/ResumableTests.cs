using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Shouldly;
using Testing.ApiTests;
using Testing.Services;

namespace Testing.SyncReverseProxy;

[Trait("Category", "Integration")]
public class ResumableTests
{
    private readonly string _baseUrl = TestingEnvironmentVariables.ResumableBaseUrl;
    private static readonly HttpClient Client = ApiTestBase.NewHttpClient().Client;

    [Theory]
    [InlineData("admin")]
    [InlineData("manager")]
    public async Task IsAvailable(string user)
    {
        var responseMessage = await Client.SendAsync(new(HttpMethod.Get,
            $"{_baseUrl}/api/v03/isAvailable?repoId={TestingEnvironmentVariables.ProjectCode}")
        {
            Headers =
            {
                Authorization = new("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{TestData.Password}")))
            }
        }, HttpCompletionOption.ResponseHeadersRead);
        var responseString = await responseMessage.Content.ReadAsStringAsync();
        responseString.ShouldBeNullOrEmpty();
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.OK);
        var headers = responseMessage.Headers.ToDictionary(kvp => kvp.Key, kvp => string.Join(',', kvp.Value), StringComparer.OrdinalIgnoreCase);
        headers.ShouldContainKeyAndValue("X-HgR-Version", "3");
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("manager")]
    public async Task IsAvailableJwtInBasicAuth(string user)
    {
        var jwt = await JwtHelper.GetJwtForUser(new(user, TestData.Password));
        jwt.ShouldNotBeNullOrEmpty();

        var responseMessage = await Client.SendAsync(new(HttpMethod.Get,
            $"{_baseUrl}/api/v03/isAvailable?repoId={TestingEnvironmentVariables.ProjectCode}")
        {
            Headers =
            {
                Authorization = new("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"bearer:{jwt}")))
            }
        }, HttpCompletionOption.ResponseHeadersRead);
        var responseString = await responseMessage.Content.ReadAsStringAsync();
        responseString.ShouldBeNullOrEmpty();
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.OK);
        var headers = responseMessage.Headers.ToDictionary(kvp => kvp.Key, kvp => string.Join(',', kvp.Value), StringComparer.OrdinalIgnoreCase);
        headers.ShouldContainKeyAndValue("X-HgR-Version", "3");
    }

    [Fact]
    public async Task WithBadUser()
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"{_baseUrl}/api/v03/isAvailable?repoId={TestingEnvironmentVariables.ProjectCode}")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"not a user:doesnt matter")))
            }
        });
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WithBadPassword()
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"{_baseUrl}/api/v03/isAvailable?repoId={TestingEnvironmentVariables.ProjectCode}")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{TestData.User}:wrong password")))
            }
        });
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WithBadNotValidProject()
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"{_baseUrl}/api/v03/isAvailable?repoId=test-not-valid")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{TestData.User}:{TestData.Password}")))
            }
        });
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WithUnauthorizedUser()
    {
        var userWithoutPermission = "user@test.com";
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"{_baseUrl}/api/v03/isAvailable?repoId={TestingEnvironmentVariables.ProjectCode}")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userWithoutPermission}:{TestData.Password}")))
            }
        });
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
