using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace FwLiteMaui;

/// <summary>
/// A short-lived loopback reverse proxy that sits between the Windows
/// <see cref="Windows.Management.Deployment.PackageManager"/> and the GitHub release asset.
///
/// Windows still owns the download/staging/install (via AddPackageByUriAsync), but by pointing
/// it at this proxy we get to observe every byte it downloads — including the HTTP Range requests
/// it uses for differential updates — so we can report real download progress. It also resolves
/// GitHub's 302 redirect once up front so range requests hit the terminal asset host directly.
/// </summary>
public sealed class UpdateDownloadProxy : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly HttpClient _upstream;
    private readonly string _originalUrl;
    private readonly ILogger _logger;
    private readonly Action<long> _onBytes;
    private readonly CancellationTokenSource _cts = new();
    private readonly Lock _urlLock = new();
    private readonly ConcurrentDictionary<Task, byte> _handlers = new();
    private string _finalUrl;
    private Task? _acceptLoop;
    private long _bytesServed;

    public Uri LocalUri { get; }

    private UpdateDownloadProxy(HttpListener listener,
        HttpClient upstream,
        string originalUrl,
        string finalUrl,
        Uri localUri,
        ILogger logger,
        Action<long> onBytes)
    {
        _listener = listener;
        _upstream = upstream;
        _originalUrl = originalUrl;
        _finalUrl = finalUrl;
        LocalUri = localUri;
        _logger = logger;
        _onBytes = onBytes;
    }

    /// <summary>
    /// Starts the proxy. Hand <see cref="LocalUri"/> to PackageManager.AddPackageByUriAsync.
    /// <paramref name="onBytes"/> is invoked with the running total of bytes streamed to Windows.
    /// </summary>
    public static async Task<UpdateDownloadProxy> StartAsync(string githubUrl,
        ILogger logger,
        Action<long> onBytes,
        CancellationToken cancellationToken = default)
    {
        // One keep-alive client for both redirect resolution and streaming; no auto-redirect so
        // range requests go straight to the terminal asset host.
        var upstream = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        upstream.DefaultRequestHeaders.UserAgent.ParseAdd("Fieldworks-Lite-UpdateProxy");

        try
        {
            var finalUrl = await ResolveFinalUrl(upstream, githubUrl, cancellationToken);

            var port = GetFreeLoopbackPort();
            // A random path segment so no other local process can stumble onto the proxy.
            var pathSegment = Guid.NewGuid().ToString("N");
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/{pathSegment}/");
            listener.Start();

            var localUri = new Uri($"http://localhost:{port}/{pathSegment}/download");
            var proxy = new UpdateDownloadProxy(listener, upstream, githubUrl, finalUrl, localUri, logger, onBytes);
            proxy._acceptLoop = Task.Run(() => proxy.AcceptLoopAsync(localUri.AbsolutePath));
            logger.LogInformation("Update download proxy listening on {LocalUri}", localUri);
            return proxy;
        }
        catch
        {
            upstream.Dispose();
            throw;
        }
    }

    private async Task AcceptLoopAsync(string expectedPath)
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception) when (_cts.IsCancellationRequested)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }

            // Windows overlaps range requests, so handle each concurrently. Track the handler so disposal
            // can wait for in-flight requests before tearing down the shared HttpClient/listener.
            var handler = Task.Run(() => HandleRequestAsync(context, expectedPath));
            _handlers[handler] = 0;
            _ = handler.ContinueWith(static (t, state) => ((ConcurrentDictionary<Task, byte>)state!).TryRemove(t, out _),
                _handlers, TaskScheduler.Default);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, string expectedPath)
    {
        try
        {
            if (!string.Equals(context.Request.Url?.AbsolutePath, expectedPath, StringComparison.Ordinal))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            await ForwardAsync(context, allowReresolve: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update proxy request failed");
            try { context.Response.StatusCode = (int)HttpStatusCode.BadGateway; }
            catch { /* response may already be committed */ }
        }
        finally
        {
            try { context.Response.Close(); }
            catch { /* ignore */ }
        }
    }

    private async Task ForwardAsync(HttpListenerContext context, bool allowReresolve)
    {
        var method = context.Request.HttpMethod;
        using var upstreamRequest = new HttpRequestMessage(new HttpMethod(method), CurrentUrl());
        var rangeHeader = context.Request.Headers["Range"];
        if (!string.IsNullOrEmpty(rangeHeader) && RangeHeaderValue.TryParse(rangeHeader, out var range))
            upstreamRequest.Headers.Range = range;

        using var upstreamResponse =
            await _upstream.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, _cts.Token);

        // The signed asset URL can expire; re-resolve from the original GitHub URL and retry once.
        if (allowReresolve && upstreamResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _logger.LogInformation("Upstream returned {Status}; re-resolving asset URL", upstreamResponse.StatusCode);
            var refreshed = await ResolveFinalUrl(_upstream, _originalUrl, _cts.Token);
            lock (_urlLock) _finalUrl = refreshed;
            await ForwardAsync(context, allowReresolve: false);
            return;
        }

        context.Response.StatusCode = (int)upstreamResponse.StatusCode;
        context.Response.KeepAlive = true;
        if (upstreamResponse.Content.Headers.ContentLength is { } contentLength)
            context.Response.ContentLength64 = contentLength;
        if (upstreamResponse.Headers.AcceptRanges.Count > 0)
            context.Response.Headers["Accept-Ranges"] = string.Join(",", upstreamResponse.Headers.AcceptRanges);
        if (upstreamResponse.Content.Headers.ContentRange is { } contentRange)
            context.Response.Headers["Content-Range"] = contentRange.ToString();

        if (method == "HEAD") return;

        await using var body = await upstreamResponse.Content.ReadAsStreamAsync(_cts.Token);
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            int read;
            while ((read = await body.ReadAsync(buffer, _cts.Token)) > 0)
            {
                await context.Response.OutputStream.WriteAsync(buffer.AsMemory(0, read), _cts.Token);
                _onBytes(Interlocked.Add(ref _bytesServed, read));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private string CurrentUrl()
    {
        lock (_urlLock) return _finalUrl;
    }

    private const int MaxRedirectHops = 5;

    // Resolve to a terminal (non-redirect) URL so range requests hit the asset host directly and the
    // response we later forward to Windows is never a 3xx (a forwarded 302 from localhost is a dead end).
    // Throws if it can't reach a terminal URL, so the caller falls back to a direct (proxy-less) install
    // and lets Windows follow the redirect itself.
    private static async Task<string> ResolveFinalUrl(HttpClient client, string url, CancellationToken cancellationToken)
    {
        for (var hop = 0; hop < MaxRedirectHops; hop++)
        {
            using var response = await SendResolveAsync(client, url, cancellationToken);
            if ((int)response.StatusCode is >= 300 and < 400)
            {
                if (response.Headers.Location is not { } location)
                    throw new HttpRequestException($"Redirect with no Location while resolving update asset ({(int)response.StatusCode})");
                url = location.IsAbsoluteUri ? location.ToString() : new Uri(new Uri(url), location).ToString();
                continue;
            }

            return url;
        }

        throw new HttpRequestException($"Exceeded {MaxRedirectHops} redirects while resolving update asset");
    }

    // HEAD is cheap for redirect discovery, but some origins reject it (405/501) while still redirecting on
    // GET. Fall back to GET (headers only; body never read) in that case. Caller disposes the response.
    private static async Task<HttpResponseMessage> SendResolveAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        var head = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url),
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (head.StatusCode is not (HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotImplemented))
            return head;

        head.Dispose();
        return await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url),
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private static int GetFreeLoopbackPort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        try
        {
            return ((IPEndPoint)probe.LocalEndpoint).Port;
        }
        finally
        {
            probe.Stop();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        try { _listener.Stop(); } catch { /* ignore */ }
        // Safe to await: these are all our own Task.Run tasks. Wait for the accept loop and any in-flight
        // request handlers (cancellation already signalled) before disposing the shared HttpClient/listener,
        // so a handler can't touch a disposed resource. Bounded so a stuck handler can't hang disposal.
#pragma warning disable VSTHRD003
        try
        {
            var pending = _handlers.Keys.ToArray();
            var drain = _acceptLoop is not null ? pending.Append(_acceptLoop) : pending;
            await Task.WhenAll(drain).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch { /* best effort: handlers swallow their own errors; timeout/cancellation is fine */ }
#pragma warning restore VSTHRD003

        ((IDisposable)_listener).Dispose();
        _upstream.Dispose();
        _cts.Dispose();
    }
}
