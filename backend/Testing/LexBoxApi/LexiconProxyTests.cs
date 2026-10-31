using FluentAssertions;
using LexBoxApi.Proxies;
using LexCore.Sync;
using Microsoft.AspNetCore.Http;

namespace Testing.LexBoxApi;

public class LexiconProxyTests
{
    [Fact]
    public async Task ForwardedRequestCarriesOnlyLexboxIdentity()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/lexicon/project/7f6d1b7a-0b5e-4f4f-9a51-6c4f2c0e2d11/entry";
        context.Request.QueryString = new QueryString("?jwt=caller-token&search=apple");
        context.Request.Headers.Authorization = "Bearer caller-token";
        context.Request.Headers.Cookie = ".LexBoxAuth=caller-cookie";
        context.Request.Headers[LexiconApi.UserIdHeader] = "spoofed-user";
        context.Request.Headers[LexiconApi.UserNameHeader] = "spoofed-name";
        context.Request.Headers[LexiconApi.ClientIdHeader] = "spoofed-client";
        var proxyRequest = new HttpRequestMessage();

        await new LexiconProxy.IdentityHeadersTransformer("user-id", "Zoë Ngũ", "lexicon-lift-tool")
            .TransformRequestAsync(context, proxyRequest, "http://fw-headless:8081/", CancellationToken.None);

        proxyRequest.Headers.Contains("Authorization").Should().BeFalse();
        proxyRequest.Headers.Contains("Cookie").Should().BeFalse();
        proxyRequest.RequestUri!.Query.Should().Be("?search=apple");
        proxyRequest.Headers.GetValues(LexiconApi.UserIdHeader).Should().Equal("user-id");
        proxyRequest.Headers.GetValues(LexiconApi.UserNameHeader).Should().Equal(Uri.EscapeDataString("Zoë Ngũ"));
        proxyRequest.Headers.GetValues(LexiconApi.ClientIdHeader).Should().Equal("lexicon-lift-tool");
    }
}
