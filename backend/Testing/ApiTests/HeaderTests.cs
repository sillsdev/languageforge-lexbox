using System.Net;
using FluentAssertions;

namespace Testing.ApiTests;

// Hits live staging (Cloudflare), so it stays out of `task test:unit` and CI unit runs.
[Trait("Category", "Integration")]
public class HeaderTests : ApiTestBase
{
    public HeaderTests()
    {
    }

    [Fact]
    public async Task CheckHeaderSizeLimit()
    {
        /*
        Since 2026-11-20 our nginx ingress (staging and production) rejects any header line over 8 KB with
        "400 Request Header Or Cookie Too Large": 8,100 chars passed and 8,192 failed every time. That matches
        nginx's default large_client_header_buffers (4 8k); local dev sets 4 16k.
        Before that, Cloudflare was the tighter limit: around 10,200 chars, varying with the header's content (520s).
        Keep this above LexAuthUser.MaxJwtLength plus the cookie name and any other cookies.
        */
        var headerSize = 8000;
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
        failStatusCodes.Should().BeEmpty();
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
