using LocalWebApp.Hubs;
using LocalWebApp.Services;
using Microsoft.OpenApi.Models;
using MiniLcm;
using Entry = LcmCrdt.Objects.Entry;

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
            var entry = await api.UpdateEntry(entryId, new UpdateObjectInput<MiniLcm.Models.Entry>().Set(e => e.Note[ws], note));
            if (entry is Entry crdtEntry)
                eventBus.NotifyEntryUpdated(crdtEntry);
        });
        return group;
    }
}
