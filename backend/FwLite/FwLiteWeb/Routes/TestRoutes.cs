﻿using FwLiteShared;
using LcmCrdt;
using FwLiteWeb.Hubs;
using FwLiteWeb.Services;
using Microsoft.OpenApi.Models;
using MiniLcm;
using MiniLcm.Models;

namespace FwLiteWeb.Routes;

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
        group.MapPost("/set-entry-note", async (IMiniLcmApi api, CurrentProjectService projectContext, ChangeEventBus eventBus, Guid entryId, string ws, string note) =>
        {
            var entry = await api.UpdateEntry(entryId, new UpdateObjectInput<Entry>().Set(e => e.Note[ws], note));
            eventBus.NotifyEntryUpdated(entry, projectContext.Project);
        });
        group.MapPost("/add-new-entry",
            async (IMiniLcmApi api, CurrentProjectService projectContext, ChangeEventBus eventBus, Entry entry) =>
            {
                var createdEntry = await api.CreateEntry(entry);
                eventBus.NotifyEntryUpdated(createdEntry, projectContext.Project);
            });
        return group;
    }
}