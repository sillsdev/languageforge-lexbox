using LexCore.Analytics;
using LexCore.ServiceInterfaces;

namespace LexSyncReverseProxy.Services;

public class ProxyEventsService(ILexProxyService lexProxyService, ILexboxAnalyticsService analytics)
{
    public async Task OnResumableRequest(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/v03/pushBundleChunk") &&
            context.Response.StatusCode == 200)
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
                        _ = analytics.TrackSendReceiveCompleted();
                    }
                }
            }
        }
    }

    public async Task OnHgRequest(HttpContext context)
    {
        if (context.Request.Query.TryGetValue("cmd", out var cmd)
            && cmd == "unbundle"
            && context.Request.GetProjectCode() is { } projectCode)
        {
            await lexProxyService.QueueProjectMetadataUpdate(projectCode);
            _ = analytics.TrackSendReceiveCompleted();
        }
    }
}
