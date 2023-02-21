using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Shouldly;

namespace Testing.SyncReverseProxy;

public class ProxyHgRequests
{
    private static readonly HttpClient Client = new();

    private void ShouldBeValidResponse(HttpResponseMessage responseMessage)
    {
        //the Basic realm part is required by the HG client, otherwise it won't request again with a basic auth header
        responseMessage.Headers.WwwAuthenticate.ToString().ShouldContain("Basic realm=\"");
    }

    //cli "C:\Program Files (x86)\SIL\FLExBridge3\Mercurial\hg" clone -U  "https://{userName}:{password}@hg-public.languageforge.org/{projectCode}" "C:\ProgramData\SIL\FieldWorks\Projects\lkj"
    [Theory]
    [InlineData("hg-public.languageforge.org")]
    [InlineData("localhost:7000")]
    public async Task TestGet(string host)
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"https://{host}/{TestData.ProjectCode}")
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
    [InlineData("hg-public.languageforge.org")]
    [InlineData("localhost:7000")]
    public async Task TestGetBadPassword(string host)
    {
        var password = "not a good password";
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"https://{host}/en-counselling-flex")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{TestData.User}:{password}")))
            }
        });
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ShouldBeValidResponse(responseMessage);
    }

    [Theory]
    [InlineData("hg-public.languageforge.org")]
    [InlineData("localhost:7000")]
    public async Task TestNoAuthResponse(string host)
    {
        var responseMessage =
            await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"https://{host}/en-counselling-flex"));
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ShouldBeValidResponse(responseMessage);
    }
}