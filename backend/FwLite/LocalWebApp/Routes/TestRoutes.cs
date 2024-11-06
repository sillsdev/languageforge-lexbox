using LocalWebApp.Hubs;
using LocalWebApp.Services;
using Microsoft.OpenApi.Models;
using MiniLcm;
using MiniLcm.Models;

namespace LocalWebApp.Routes;

public static class TestRoutes
{
    public static IEndpointConventionBuilder MapTest(this WebApplication app)
    {
        var group = app.MapGroup("/api/test/{project}").WithOpenApi(operation =>
        {
            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = CrdtMiniLcmApiHub.ProjectRouteKey,
                In = ParameterLocation.Path,
                Required = true
            });
            return operation;
        });
        group.MapGet("/entries",
            (IMiniLcmApi api) =>
            {
                return api.GetEntries();
            });
        group.MapPost("/set-entry-note", async (IMiniLcmApi api, ChangeEventBus eventBus, Guid entryId, string ws, string note) =>
        {
            var entry = await api.UpdateEntry(entryId, new UpdateObjectInput<Entry>().Set(e => e.Note[ws], note));
            eventBus.NotifyEntryUpdated(entry);
        });
        group.MapPost("/add-new-entry",
            async (IMiniLcmApi api, ChangeEventBus eventBus, Entry entry) =>
            {
                var createdEntry = await api.CreateEntry(entry);
                eventBus.NotifyEntryUpdated(createdEntry);
            });
        return group;
    }
}
