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
        group.MapGet("/", (
            HistoryService historyService,
            int skip,
            int take,
            string? authorName,
            bool authorMissing,
            bool excludeFieldWorks) =>
        {
            var hasFilter = authorName is not null || authorMissing || excludeFieldWorks;
            var filter = hasFilter ? new ProjectActivityFilter(authorName, authorMissing, excludeFieldWorks) : null;
            return historyService.ProjectActivity(skip, take, filter);
        });
        group.MapGet("/authors", (HistoryService historyService) => historyService.GetAuthors());
        return group;
    }
}
