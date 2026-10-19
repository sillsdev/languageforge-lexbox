using System.Net;
using Microsoft.Extensions.Primitives;

namespace LexBoxApi.Cloudflare;

/// <summary>
/// In production the chain is visitor → Cloudflare → ingress → lexbox, so the raw socket peer is the
/// ingress, not Cloudflare. <c>UseForwardedHeaders</c> (trusting our own proxy network) unwinds the ingress
/// hop, leaving <see cref="ConnectionInfo.RemoteIpAddress"/> as the Cloudflare edge IP — still not the
/// visitor. Cloudflare puts the real visitor in the <c>CF-Connecting-IP</c> header; this middleware replaces
/// <see cref="ConnectionInfo.RemoteIpAddress"/> with it, but only when the current remote IP is inside a
/// published Cloudflare range, so a direct client can't spoof the header.
///
/// It must run AFTER <c>UseForwardedHeaders</c> — that both exposes the Cloudflare edge IP to validate and
/// provides the plain <c>X-Forwarded-For</c> fallback for local dev and any non-Cloudflare path. Downstream
/// code should read only <see cref="ConnectionInfo.RemoteIpAddress"/>.
/// </summary>
public sealed class CloudflareClientIpMiddleware(
    RequestDelegate next,
    CloudflareIpRangesProvider ranges,
    ILogger<CloudflareClientIpMiddleware> logger)
{
    public const string ConnectingIpHeader = "CF-Connecting-IP";

    public async Task InvokeAsync(HttpContext context)
    {
        var headerValue = context.Request.Headers[ConnectingIpHeader];
        // Only touch requests that actually carry the Cloudflare header; this is also the only path that
        // triggers the (once-only, lazy) fetch of Cloudflare's IP ranges.
        if (!StringValues.IsNullOrEmpty(headerValue)
            && context.Connection.RemoteIpAddress is { } peer
            && IPAddress.TryParse(headerValue.ToString(), out var clientIp))
        {
            var normalizedPeer = peer.IsIPv4MappedToIPv6 ? peer.MapToIPv4() : peer;
            var cloudflareRanges = await ranges.GetRangesAsync();
            if (cloudflareRanges.Any(range => range.Contains(normalizedPeer)))
            {
                context.Connection.RemoteIpAddress = clientIp;
            }
            else
            {
                logger.LogWarning("Ignoring {Header} from non-Cloudflare peer {Peer}", ConnectingIpHeader, peer);
            }
        }

        await next(context);
    }
}

/// <summary>
/// Supplies Cloudflare's published IP ranges, fetched from cloudflare.com the first time they're needed
/// and cached for the lifetime of the process. If the fetch fails the built-in fallback list is used.
/// </summary>
public sealed class CloudflareIpRangesProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<CloudflareIpRangesProvider> logger)
{
    public const string HttpClientName = "CloudflareIps";
    private const string Ipv4Url = "https://www.cloudflare.com/ips-v4";
    private const string Ipv6Url = "https://www.cloudflare.com/ips-v6";

    private readonly object _gate = new();
    private Task<IReadOnlyList<IPNetwork>>? _rangesTask;

    /// <summary>Cloudflare's ranges, fetching them exactly once on first call.</summary>
    public Task<IReadOnlyList<IPNetwork>> GetRangesAsync()
    {
        if (_rangesTask is not null) return _rangesTask;
        lock (_gate)
        {
            // FetchRangesAsync never throws, so a faulted task is never cached.
            return _rangesTask ??= FetchRangesAsync();
        }
    }

    private async Task<IReadOnlyList<IPNetwork>> FetchRangesAsync()
    {
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            var v4 = await client.GetStringAsync(Ipv4Url);
            var v6 = await client.GetStringAsync(Ipv6Url);
            var fetched = ParseCidrs(v4).Concat(ParseCidrs(v6)).ToList();
            if (fetched.Count > 0)
            {
                logger.LogInformation("Loaded {Count} Cloudflare IP ranges", fetched.Count);
                return fetched;
            }
            logger.LogWarning("Cloudflare IP range lists were empty; using built-in fallback ranges");
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to fetch Cloudflare IP ranges; using built-in fallback ranges");
        }

        return FallbackRanges;
    }

    private static IEnumerable<IPNetwork> ParseCidrs(string text)
    {
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (IPNetwork.TryParse(line, out var network))
                yield return network;
        }
    }

    // https://www.cloudflare.com/ips/ — used only if the live lists can't be fetched.
    private static readonly IReadOnlyList<IPNetwork> FallbackRanges =
    [
        IPNetwork.Parse("173.245.48.0/20"),
        IPNetwork.Parse("103.21.244.0/22"),
        IPNetwork.Parse("103.22.200.0/22"),
        IPNetwork.Parse("103.31.4.0/22"),
        IPNetwork.Parse("141.101.64.0/18"),
        IPNetwork.Parse("108.162.192.0/18"),
        IPNetwork.Parse("190.93.240.0/20"),
        IPNetwork.Parse("188.114.96.0/20"),
        IPNetwork.Parse("197.234.240.0/22"),
        IPNetwork.Parse("198.41.128.0/17"),
        IPNetwork.Parse("162.158.0.0/15"),
        IPNetwork.Parse("104.16.0.0/13"),
        IPNetwork.Parse("104.24.0.0/14"),
        IPNetwork.Parse("172.64.0.0/13"),
        IPNetwork.Parse("131.0.72.0/22"),
        IPNetwork.Parse("2400:cb00::/32"),
        IPNetwork.Parse("2606:4700::/32"),
        IPNetwork.Parse("2803:f800::/32"),
        IPNetwork.Parse("2405:b500::/32"),
        IPNetwork.Parse("2405:8100::/32"),
        IPNetwork.Parse("2a06:98c0::/29"),
        IPNetwork.Parse("2c0f:f248::/32"),
    ];
}

public static class CloudflareClientIpExtensions
{
    public static IServiceCollection AddCloudflareClientIp(this IServiceCollection services)
    {
        services.AddHttpClient(CloudflareIpRangesProvider.HttpClientName,
            client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddSingleton<CloudflareIpRangesProvider>();
        return services;
    }

    /// <summary>Rewrites the remote IP from a validated <c>CF-Connecting-IP</c>. Place after UseForwardedHeaders.</summary>
    public static IApplicationBuilder UseCloudflareClientIp(this IApplicationBuilder app) =>
        app.UseMiddleware<CloudflareClientIpMiddleware>();
}
