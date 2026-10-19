using FwLiteShared;
using FwLiteShared.Events;
using LcmCrdt;
using FwLiteWeb.Services;
using Microsoft.OpenApi;
using MiniLcm;
using MiniLcm.Models;

namespace FwLiteWeb.Routes;

public static class TestRoutes
{
    public static IEndpointConventionBuilder MapTest(this WebApplication app)
    {
        var group = app.MapGroup("/api/test/{project}").AddOpenApiOperationTransformer((operation, _, _) =>
        {
            operation.Parameters?.Add(new OpenApiParameter()
            {
                Name = RouteKeys.Project,
                In = ParameterLocation.Path,
                Required = true
            });
            return Task.CompletedTask;
        });
        group.MapGet("/entries",
            (IMiniLcmApi api) =>
            {
                return api.GetEntries();
            });
        group.MapPost("/set-entry-note", async (IMiniLcmApi api, CurrentProjectService projectContext, ProjectEventBus eventBus, Guid entryId, string ws, string note) =>
        {
            var entry = await api.UpdateEntry(entryId, new UpdateObjectInput<Entry>().Set(e => e.Note[ws], new RichString(note)));
            eventBus.PublishEntryChanged(projectContext.Project, entry.Id);
        });
        group.MapPost("/add-new-entry",
            async (IMiniLcmApi api, CurrentProjectService projectContext, ProjectEventBus eventBus, Entry entry) =>
            {
                var createdEntry = await api.CreateEntry(entry, CreateEntryOptions.WithMainPublication);
                eventBus.PublishEntryChanged(projectContext.Project, createdEntry.Id);
            });
        return group;
    }
}
