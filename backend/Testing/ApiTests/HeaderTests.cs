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
        /*
        From testing done in November 2023, for 300 iterations a header size of:
        10,210 failed 7 times
        10,200 failed 1 time
        10,195 failed 0 times
        */
        var headerSize = 10195;
        var iterations = 10; // a slightly bigger sample set, so we're more confident that we're in the clear
        var failStatusCodes = new List<HttpStatusCode>();
        for (var i = 0; i < iterations; i++)
        {
            var response = await HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "https://staging.languagedepot.org/api/healthz")
                {
                    Headers = { { "test", RandomString(headerSize) } }
                });

            if (response.StatusCode != HttpStatusCode.OK) failStatusCodes.Add(response.StatusCode);
        }
        failStatusCodes.ShouldBeEmpty();
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
