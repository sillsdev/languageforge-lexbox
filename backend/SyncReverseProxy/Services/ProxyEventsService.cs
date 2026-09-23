using LexCore.Analytics;
using LexCore.ServiceInterfaces;

namespace LexSyncReverseProxy.Services;

public class ProxyEventsService(ILexProxyService lexProxyService, ILexboxAnalyticsService analytics)
{
    public async Task OnResumableRequest(HttpContext context)
    {
        if (context.Response.StatusCode != 200) return;

        if (context.Request.Path.StartsWithSegments("/api/v03/pushBundleChunk"))
        {
            if (context.Request.Query.TryGetValue("chunksize", out var chunkSizeStr) &&
                context.Request.Query.TryGetValue("offset", out var offsetStr) &&
                context.Request.Query.TryGetValue("bundlesize", out var bundleSizeStr))
            {
                if (int.TryParse(chunkSizeStr, out var chunkSize) &&
                    int.TryParse(offsetStr, out var offset) &&
                    int.TryParse(bundleSizeStr, out var bundleSize))
                {
                    if (offset + chunkSize >= bundleSize &&
                        context.Request.GetProjectCode() is { } projectCode)
                    {
                        // Last chunk, so record updated last-changed date
                        await lexProxyService.QueueProjectMetadataUpdate(projectCode);
                        _ = analytics.TrackSendReceive(ILexboxAnalyticsService.SendDirection);
                    }
                }
            }
        }
        else if (context.Request.Path.StartsWithSegments("/api/v03/pullBundleChunk"))
        {
            // A pull streams the bundle over many chunk requests; count one send/receive per pull by only
            // firing on the first chunk (offset 0). A pull doesn't change the repo, so no metadata update.
            if (context.Request.Query.TryGetValue("offset", out var offsetStr) &&
                int.TryParse(offsetStr, out var offset) &&
                offset == 0)
            {
                _ = analytics.TrackSendReceive(ILexboxAnalyticsService.ReceiveDirection);
            }
        }
    }

    public async Task OnHgRequest(HttpContext context)
    {
        if (!context.Request.Query.TryGetValue("cmd", out var cmd)) return;
        if (context.Request.GetProjectCode() is not { } projectCode) return;

        if (cmd == "unbundle")
        {
            await lexProxyService.QueueProjectMetadataUpdate(projectCode);
            _ = analytics.TrackSendReceive(ILexboxAnalyticsService.SendDirection);
        }
        else if (cmd == "getbundle")
        {
            // Fetch (pull). Doesn't change the repo, so no metadata update — just track the send/receive.
            _ = analytics.TrackSendReceive(ILexboxAnalyticsService.ReceiveDirection);
        }
    }
}
