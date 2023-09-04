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

    public ValueTask OnResumableRequest(HttpContext context)
    {
        //todo update project last change
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHgRequest(HttpContext context)
    {
        if (context.Request.Query.TryGetValue("cmd", out var cmd)
            && cmd == "unbundle"
            && context.Request.RouteValues.TryGetValue(ProxyConstants.HgProjectCodeRouteKey, out var projectCodeObj))
        {
            var projectCode = projectCodeObj?.ToString() ?? null;
            if (projectCode is not null)
            {
                //discard, we don't care about the result
                var _ = Task.Run(() => _lexProxyService.RefreshProjectLastChange(projectCode));
            }
        }

        return ValueTask.CompletedTask;
    }
}
