using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using LexBoxApi.Auth;
using Shouldly;
using Testing.Services;

namespace Testing.SyncReverseProxy;

[Trait("Category", "Integration")]
public class ProxyHgRequests
{
    private string _baseUrl = TestingEnvironmentVariables.StandardHgBaseUrl;
    private static readonly HttpClientHandler Handler = new HttpClientHandler()
    {
        UseCookies = false
    };
    private static readonly HttpClient Client = new(Handler);

    private void ShouldBeValidResponse(HttpResponseMessage responseMessage)
    {
        //the Basic realm part is required by the HG client, otherwise it won't request again with a basic auth header
        responseMessage.Headers.WwwAuthenticate.ToString().ShouldContain("Basic realm=\"");
    }

    [Fact]
    public async Task TestGet()
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"{_baseUrl}/{TestingEnvironmentVariables.ProjectCode}")
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
    public async Task TestGetPrefixHg()
    {
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"{_baseUrl}/hg/{TestingEnvironmentVariables.ProjectCode}")
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
    public async Task TestGetWithJwtInBasicAuth()
    {
        var response = await Client.PostAsJsonAsync(
            $"{_baseUrl}/api/login",
            new Dictionary<string, object>
            {
                { "password",  TestData.Password }, { "emailOrUsername", TestData.User }, { "preHashedPassword", false }
            });
        response.EnsureSuccessStatusCode();
        var cookies = response.Headers.GetValues("Set-Cookie");
        var cookieContainer = new CookieContainer();
        cookieContainer.SetCookies(response.RequestMessage!.RequestUri!, cookies.Single());
        var authCookie = cookieContainer.GetAllCookies().FirstOrDefault(c => c.Name == AuthKernel.AuthCookieName);
        authCookie.ShouldNotBeNull();
        var jwt = authCookie.Value;
        jwt.ShouldNotBeNullOrEmpty();

        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"{_baseUrl}/{TestingEnvironmentVariables.ProjectCode}")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"bearer:{jwt}")))
            }
        });
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TestGetBadPassword()
    {
        var password = "not a good password";
        var responseMessage = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"{_baseUrl}/{TestingEnvironmentVariables.ProjectCode}")
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
                $"{_baseUrl}/{TestingEnvironmentVariables.ProjectCode}"));
        responseMessage.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ShouldBeValidResponse(responseMessage);
    }

    [Fact]
    public async Task SimpleClone()
    {
        var projectCode = TestingEnvironmentVariables.ProjectCode;
        // projectCode = "elawa-dev-flex";
        var auth = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"admin:{TestData.Password}")));
        var batchRequest = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/{projectCode}?cmd=batch")
        {
            Headers = { Authorization = auth }
        };
        batchRequest.Headers.Add("x-hgarg-1", "cmds=heads+%3Bknown+nodes%3D");
        var batchResponse = await Client.SendAsync(batchRequest);
        batchResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var batchBody = await batchResponse.Content.ReadAsStringAsync();
        batchBody.ShouldEndWith(";");
        var heads = batchBody.Split('\n')[^2];

        var getBundleRequest = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/{projectCode}?cmd=getbundle")
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
