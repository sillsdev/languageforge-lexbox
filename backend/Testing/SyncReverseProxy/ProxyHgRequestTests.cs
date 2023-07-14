using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Shouldly;
using Testing.Services;

namespace Testing.SyncReverseProxy;

[Trait("Category", "Integration")]
public class ProxyHgRequests
{
    private string _host = TestingEnvironmentVariables.StandardHgHostname;
    private static readonly HttpClient Client = new();

    private void ShouldBeValidResponse(HttpResponseMessage responseMessage)
    {
        //the Basic realm part is required by the HG client, otherwise it won't request again with a basic auth header
        responseMessage.Headers.WwwAuthenticate.ToString().ShouldContain("Basic realm=\"");
    }

    //cli "C:\Program Files (x86)\SIL\FLExBridge3\Mercurial\hg" clone -U  "https://{userName}:{password}@hg-public.languageforge.org/{projectCode}" "C:\ProgramData\SIL\FieldWorks\Projects\lkj"
    [Fact]
    public async Task TestGet()
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"http://{_host}/{TestData.ProjectCode}")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{TestData.User}:{TestData.Password}")))
            }
        });
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TestGetBadPassword()
    {
        var password = "not a good password";
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"http://{_host}/{TestData.ProjectCode}")
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

    [Fact]
    public async Task TestNoAuthResponse()
    {
        var responseMessage =
            await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"http://{_host}/{TestData.ProjectCode}"));
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ShouldBeValidResponse(responseMessage);
    }
}
