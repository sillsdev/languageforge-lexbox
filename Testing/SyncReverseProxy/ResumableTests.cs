using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Shouldly;

namespace Testing.SyncReverseProxy;

public class ResumableTests
{
    private static readonly HttpClient Client = new();

    [Theory]
    [InlineData("resumable.languageforge.org")]
    [InlineData("localhost:7000")]
    public async Task IsAvailable(string host)
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"https://{host}/api/v03/isAvailable?repoId=en-counselling-flex")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{TestData.User}:{TestData.Password}")))
            }
        });
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}