using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Shouldly;

namespace Testing.SyncReverseProxy;

public class ResumableTests
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(3)
    };

    [Theory]
    [InlineData("resumable.languageforge.org")]
    [InlineData("localhost:7000")]
    public async Task IsAvailable(string host)
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"https://{host}/api/v03/isAvailable?repoId={TestData.ProjectCode}")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{TestData.User}:{TestData.Password}")))
            }
        });
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
    
    [Theory]
    [InlineData("resumable.languageforge.org")]
    [InlineData("localhost:7000")]
    public async Task WithBadUser(string host)
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"https://{host}/api/v03/isAvailable?repoId={TestData.ProjectCode}")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"not a user:doesnt matter")))
            }
        });
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
    
    [Theory]
    [InlineData("resumable.languageforge.org")]
    [InlineData("localhost:7000")]
    public async Task WithBadPassword(string host)
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"https://{host}/api/v03/isAvailable?repoId={TestData.ProjectCode}")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{TestData.User}:wrong password")))
            }
        });
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
    
    [Theory]
    [InlineData("resumable.languageforge.org")]
    [InlineData("localhost:7000")]
    public async Task WithBadNotValidProject(string host)
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"https://{host}/api/v03/isAvailable?repoId=test-not-valid")
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