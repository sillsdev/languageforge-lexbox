using Humanizer;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Db;
using LcmCrdt;
using LcmCrdt.Changes;
using FwLiteWeb.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SIL.Harmony.Entities;

namespace FwLiteWeb.Routes;

public static class ActivityRoutes
{
    public static IEndpointConventionBuilder MapActivities(this WebApplication app)
    {
        var group = app.MapGroup("/api/activity/{project}").WithOpenApi(operation =>
        {
            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = CrdtMiniLcmApiHub.ProjectRouteKey,
                In = ParameterLocation.Path,
                Required = true
            });
            return operation;
        });
        group.MapGet("/", (HistoryService historyService) => historyService.ProjectActivity());
        return group;
    }
}
