using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FwLiteMaui.Tests;

#if WINDOWS
public class UpdateDownloadProxyTests
{
    // Deterministic payload the fake upstream serves, with byte-range support.
    private static readonly byte[] Payload = Enumerable.Range(0, 100_000).Select(i => (byte)(i % 251)).ToArray();

    [Fact]
    public async Task ForwardsFullDownloadAndReportsByteTotal()
    {
        await using var upstream = new FakeUpstream(Payload);
        long reported = 0;
        await using var proxy = await UpdateDownloadProxy.StartAsync(upstream.Url, NullLogger.Instance, total => reported = total);

        using var client = new HttpClient();
        var body = await client.GetByteArrayAsync(proxy.LocalUri);

        body.Should().Equal(Payload);
        reported.Should().Be(Payload.Length);
    }

    [Fact]
    public async Task ForwardsRangeRequestAsPartialContent()
    {
        await using var upstream = new FakeUpstream(Payload);
        await using var proxy = await UpdateDownloadProxy.StartAsync(upstream.Url, NullLogger.Instance, _ => { });

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, proxy.LocalUri);
        request.Headers.Range = new RangeHeaderValue(10, 19);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
        response.Content.Headers.ContentRange!.From.Should().Be(10);
        response.Content.Headers.ContentRange!.To.Should().Be(19);
        var body = await response.Content.ReadAsByteArrayAsync();
        body.Should().Equal(Payload[10..20]);
    }

    [Fact]
    public async Task ReturnsNotFoundForUnknownPath()
    {
        await using var upstream = new FakeUpstream(Payload);
        await using var proxy = await UpdateDownloadProxy.StartAsync(upstream.Url, NullLogger.Instance, _ => { });

        var wrongPath = new Uri(proxy.LocalUri, "wrong");
        using var client = new HttpClient();
        using var response = await client.GetAsync(wrongPath);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Minimal HTTP origin that serves a byte array with Range/HEAD support.</summary>
    private sealed class FakeUpstream : IAsyncDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly byte[] _payload;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loop;

        public string Url { get; }

        public FakeUpstream(byte[] payload)
        {
            _payload = payload;
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            Url = $"http://localhost:{port}/asset";
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();
            _loop = Task.Run(AcceptLoopAsync);
        }

        private async Task AcceptLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try { ctx = await _listener.GetContextAsync(); }
                catch { break; }
                _ = Task.Run(() => Handle(ctx));
            }
        }

        private async Task Handle(HttpListenerContext ctx)
        {
            try
            {
                ctx.Response.Headers["Accept-Ranges"] = "bytes";
                var rangeHeader = ctx.Request.Headers["Range"];
                if (!string.IsNullOrEmpty(rangeHeader) && RangeHeaderValue.TryParse(rangeHeader, out var range))
                {
                    var from = (int)(range.Ranges.First().From ?? 0);
                    var to = (int)(range.Ranges.First().To ?? _payload.Length - 1);
                    var length = to - from + 1;
                    ctx.Response.StatusCode = (int)HttpStatusCode.PartialContent;
                    ctx.Response.Headers["Content-Range"] = $"bytes {from}-{to}/{_payload.Length}";
                    ctx.Response.ContentLength64 = length;
                    if (ctx.Request.HttpMethod != "HEAD")
                        await ctx.Response.OutputStream.WriteAsync(_payload.AsMemory(from, length));
                }
                else
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                    ctx.Response.ContentLength64 = _payload.Length;
                    if (ctx.Request.HttpMethod != "HEAD")
                        await ctx.Response.OutputStream.WriteAsync(_payload);
                }
            }
            finally
            {
                ctx.Response.Close();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            try { _listener.Stop(); } catch { /* ignore */ }
#pragma warning disable VSTHRD003 // _loop is our own Task.Run started in the ctor
            try { await _loop; } catch { /* ignore */ }
#pragma warning restore VSTHRD003
            ((IDisposable)_listener).Dispose();
            _cts.Dispose();
        }
    }
}
#endif
