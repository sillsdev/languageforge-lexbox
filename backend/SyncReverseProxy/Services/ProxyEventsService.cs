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

    public ValueTask AfterRequest(HttpContext context, IReverseProxyFeature reverseProxyFeature)
    {
        switch (reverseProxyFeature.Route.Config.RouteId)
        {
            case "hg":
                return HandleHgRequest(context);
        }

        return ValueTask.CompletedTask;
    }

    private ValueTask HandleHgRequest(HttpContext context)
    {
        if (context.Request.Query.TryGetValue("cmd", out var cmd))
        {
            if (cmd == "unbundle")
            {
                if (context.Request.RouteValues.TryGetValue("project-code", out var projectCodeObj))
                {
                    var projectCode = projectCodeObj?.ToString() ?? null;
                    if (projectCode is not null)
                    {
                        Task.Run(() => _lexProxyService.RefreshProjectLastChange(projectCode));
                    }
                }
            }
        }
        return ValueTask.CompletedTask;
    }
}