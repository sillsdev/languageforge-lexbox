using LexCore.ServiceInterfaces;
using Yarp.ReverseProxy.Model;

namespace LexSyncReverseProxy.Services;

public class ProxyEventsService
{
    private readonly ILexProxyService _lexProxyService;

    public ProxyEventsService(ILexProxyService lexProxyService)
    {
        _lexProxyService = lexProxyService;
    }

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
                    if (offset + chunkSize >= bundleSize)
                    {
                        // Last chunk, so record updated last-changed date
                        var projectCode = context.Request.GetProjectCode();
                        if (projectCode is not null)
                        {
                            await _lexProxyService.RefreshProjectLastChange(projectCode);
                        }
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
            await _lexProxyService.RefreshProjectLastChange(projectCode);
        }
    }
}
