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
            int skip = 0,
            int take = 100,
            string? authorId = null,
            string? authorName = null,
            bool excludeFieldWorks = false,
            ActivitySort sort = ActivitySort.NewestFirst) =>
            historyService.ProjectActivity(skip, take, new ActivityQuery(authorId, authorName, excludeFieldWorks, sort)));
        group.MapGet("/authors", (HistoryService historyService) => historyService.ListActivityAuthors());
        group.MapGet("/change-types", (HistoryService historyService) => historyService.ListActivityChangeTypes());
        return group;
    }
}
