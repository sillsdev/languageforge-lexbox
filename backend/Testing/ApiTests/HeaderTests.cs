using System.Net;
using Shouldly;

namespace Testing.ApiTests;

public class HeaderTests : ApiTestBase
{
    public HeaderTests()
    {
    }

    [Fact]
    public async Task CheckCloudflareHeaderSizeLimit()
    {
        //from testing done in November 2023, we started getting errors at 10,225 chars
        int headerSize = 10210;

        var response = await HttpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://staging.languagedepot.org/api/healthz")
            {
                Headers = { { "test", RandomString(headerSize) } }
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"header size: {headerSize}");
    }

    private string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        Span<char> randomArray = stackalloc char[length];
        for (int i = 0; i < length; i++)
        {
            randomArray[i] = chars[Random.Shared.Next(chars.Length)];
        }

        return new string(randomArray);
    }
}
