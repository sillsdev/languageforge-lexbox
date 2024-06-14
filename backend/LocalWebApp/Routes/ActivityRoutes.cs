using Crdt.Changes;
using Crdt.Core;
using Crdt.Db;
using LocalWebApp.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace LocalWebApp.Routes;

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
        group.MapGet("/",
            (CrdtDbContext dbcontext) =>
            {
                return dbcontext.Commits.DefaultOrder().Take(10).Select(c => new Activity(c.Id, c.HybridDateTime.DateTime, ChangeName(c.ChangeEntities), c.ChangeEntities)).AsAsyncEnumerable();
            });
        return group;
    }

    private static string ChangeName(List<ChangeEntity<IChange>> changeEntities)
    {
        return changeEntities switch
        {
            { Count: 0 } => "No changes",
            { Count: 1 } => changeEntities[0].Change.GetType().Name,
            _ => "Multiple changes"
        };
    }

    public record Activity(Guid CommitId, DateTimeOffset Timestamp, string ChangeName, List<ChangeEntity<IChange>> Changes);
}
