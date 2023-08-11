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
            $"http://{_host}/{TestingEnvironmentVariables.ProjectCode}")
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
            $"http://{_host}/{TestingEnvironmentVariables.ProjectCode}")
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
            await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"http://{_host}/{TestingEnvironmentVariables.ProjectCode}"));
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ShouldBeValidResponse(responseMessage);
    }

    [Fact]
    public async Task SimpleClone()
    {
        var projectCode = TestingEnvironmentVariables.ProjectCode;
        var host = TestingEnvironmentVariables.StandardHgHostname;
        // var host = "localhost:60978";

        var auth = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{TestData.User}:{TestData.Password}")));
        var batchRequest = new HttpRequestMessage(HttpMethod.Get, $"http://{host}/{projectCode}?cmd=batch")
        {
            Headers = { Authorization = auth }
        };
        batchRequest.Headers.Add("x-hgarg-1", "cmds=heads+%3Bknown+nodes%3D");
        var batchResponse = await Client.SendAsync(batchRequest);
        var batchBody = await batchResponse.Content.ReadAsStringAsync();
        batchBody.ShouldEndWith(";");
        var heads = batchBody.Split('\n')[^2];

        var getBundleRequest = new HttpRequestMessage(HttpMethod.Get, $"http://{host}/{projectCode}?cmd=getbundle")
        {
            Headers = { Authorization = auth },
        };

        getBundleRequest.Headers.Add("x-hgarg-1", $"common=0000000000000000000000000000000000000000&heads={heads}");
        Directory.CreateDirectory("test");
        await using var fileStream = File.Open("test/simpleCloneResponse", FileMode.Create);

        //act
        var bundleResponse = await Client.SendAsync(getBundleRequest, HttpCompletionOption.ResponseHeadersRead);
        bundleResponse.EnsureSuccessStatusCode();

        await bundleResponse.Content.CopyToAsync(fileStream);
    }
}
