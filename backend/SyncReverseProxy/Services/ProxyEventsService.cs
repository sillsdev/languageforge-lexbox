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
        if (context.Request.Path.StartsWithSegments("/api/v03/finishPushBundle"))
        {
            var projectCode = context.Request.GetProjectCode();
            if (projectCode is not null)
            {
                await _lexProxyService.RefreshProjectLastChange(projectCode);
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
