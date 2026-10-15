using System.Net;
using System.Text;
using FluentAssertions;
using LexBoxApi.Cloudflare;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Testing.LexBoxApi.Cloudflare;

public class CloudflareClientIpMiddlewareTests
{
    private const string CloudflareRangeIp = "104.16.0.1"; // inside 104.16.0.0/13
    private const string VisitorIp = "203.0.113.7";

    [Fact]
    public async Task ReplacesRemoteIp_WhenPeerIsCloudflareAndHeaderPresent()
    {
        var context = BuildContext(peer: CloudflareRangeIp, connectingIp: VisitorIp);
        var middleware = BuildMiddleware(out _);

        await middleware.InvokeAsync(context);

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(VisitorIp));
    }

    [Fact]
    public async Task LeavesRemoteIp_WhenPeerIsNotCloudflare()
    {
        var context = BuildContext(peer: "10.0.0.5", connectingIp: VisitorIp);
        var middleware = BuildMiddleware(out _);

        await middleware.InvokeAsync(context);

        // A direct (non-Cloudflare) client can't spoof the header.
        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("10.0.0.5"));
    }

    [Fact]
    public async Task LeavesRemoteIp_WhenNoCloudflareHeader()
    {
        var context = BuildContext(peer: CloudflareRangeIp, connectingIp: null);
        var middleware = BuildMiddleware(out var handler);

        await middleware.InvokeAsync(context);

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(CloudflareRangeIp));
        // No header => no need to know the Cloudflare ranges, so they are never fetched.
        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task FetchesCloudflareRangesOnlyOnce()
    {
        var provider = BuildProvider(out var handler);
        var middleware = new CloudflareClientIpMiddleware(
            _ => Task.CompletedTask, provider, NullLogger<CloudflareClientIpMiddleware>.Instance);

        for (var i = 0; i < 3; i++)
            await middleware.InvokeAsync(BuildContext(peer: CloudflareRangeIp, connectingIp: VisitorIp));

        // One request for the v4 list and one for the v6 list, regardless of how many requests we serve.
        handler.RequestCount.Should().Be(2);
    }

    private static CloudflareClientIpMiddleware BuildMiddleware(out StubHandler handler)
    {
        var provider = BuildProvider(out handler);
        return new CloudflareClientIpMiddleware(
            _ => Task.CompletedTask, provider, NullLogger<CloudflareClientIpMiddleware>.Instance);
    }

    private static CloudflareIpRangesProvider BuildProvider(out StubHandler handler)
    {
        var capturedHandler = new StubHandler();
        handler = capturedHandler;
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(CloudflareIpRangesProvider.HttpClientName))
            .Returns(() => new HttpClient(capturedHandler));
        return new CloudflareIpRangesProvider(factory.Object, NullLogger<CloudflareIpRangesProvider>.Instance);
    }

    private static DefaultHttpContext BuildContext(string peer, string? connectingIp)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        if (connectingIp is not null)
            context.Request.Headers[CloudflareClientIpMiddleware.ConnectingIpHeader] = connectingIp;
        return context;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            var body = request.RequestUri!.AbsoluteUri.Contains("ips-v6") ? "2400:cb00::/32" : "104.16.0.0/13";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8)
            });
        }
    }
}
