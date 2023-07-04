using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Shouldly;
using Testing.Services;

namespace Testing.SyncReverseProxy;

[Trait("Category", "Integration")]
public class ResumableTests
{
    private readonly string _host = TestingEnvironmentVariables.ResumableHgHostname;
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(3)
    };

    [Fact]
    public async Task IsAvailable()
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"http://{_host}/api/v03/isAvailable?repoId={TestData.ProjectCode}")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{TestData.User}:{TestData.Password}")))
            }
        });
        var responseString = await responseMessage.Content.ReadAsStringAsync();
        responseString.ShouldBeNullOrEmpty();
        responseMessage.Headers.GetValues("X-HgR-Version").ShouldHaveSingleItem().ShouldBe("3");
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WithBadUser()
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"http://{_host}/api/v03/isAvailable?repoId={TestData.ProjectCode}")
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
            $"http://{_host}/api/v03/isAvailable?repoId={TestData.ProjectCode}")
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
            $"http://{_host}/api/v03/isAvailable?repoId=test-not-valid")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{TestData.User}:{TestData.Password}")))
            }
        });
        responseMessage.StatusCode.ShouldBeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }
}
