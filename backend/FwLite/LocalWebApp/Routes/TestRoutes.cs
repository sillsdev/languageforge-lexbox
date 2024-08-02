using SIL.Harmony.Core;
using SIL.Harmony.Db;
using LocalWebApp.Hubs;
using Microsoft.EntityFrameworkCore;
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
            (ILexboxApi api) =>
            {
                return api.GetEntries();
            });
        return group;
    }
}
