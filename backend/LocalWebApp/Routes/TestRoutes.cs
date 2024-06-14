using Crdt.Core;
using Crdt.Db;
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
            (CrdtDbContext dbContext, ILexboxApi api) =>
            {
                return api.GetEntries();
                return dbContext.Set<Entry>().Take(1000).AsAsyncEnumerable();
            });
        return group;
    }
}
