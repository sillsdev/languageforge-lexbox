using LcmCrdt;
using FwLiteWeb.Hubs;
using Microsoft.OpenApi;

namespace FwLiteWeb.Routes;

public static class ActivityRoutes
{
    public static IEndpointConventionBuilder MapActivities(this WebApplication app)
    {
        var group = app.MapGroup("/api/activity/{project}").AddOpenApiOperationTransformer((operation, _, _) =>
        {
            operation.Parameters?.Add(new OpenApiParameter()
            {
                Name = CrdtMiniLcmApiHub.ProjectRouteKey,
                In = ParameterLocation.Path,
                Required = true
            });
            return Task.CompletedTask;
        });
        group.MapGet("/", (HistoryService historyService, int skip, int take) => historyService.ProjectActivity(skip, take));
        return group;
    }
}
