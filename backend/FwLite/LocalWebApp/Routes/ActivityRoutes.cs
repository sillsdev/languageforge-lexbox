using Humanizer;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Db;
using LcmCrdt;
using LcmCrdt.Changes;
using LocalWebApp.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SIL.Harmony.Entities;

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
            (ICrdtDbContext dbcontext) =>
            {
                return dbcontext.Commits
                    .DefaultOrderDescending()
                    .Take(20)
                    .Select(c => new Activity(c.Id, c.HybridDateTime.DateTime, c.ChangeEntities))
                    .AsAsyncEnumerable();
            });
        return group;
    }

    private static string ChangeName(List<ChangeEntity<IChange>> changeEntities)
    {
        return changeEntities switch
        {
            { Count: 0 } => "No changes",
            { Count: 1 } => changeEntities[0].Change switch
            {
                //todo call JsonPatchChange.Summarize() instead of this
                IChange change when change.GetType().Name.StartsWith("JsonPatchChange") => "Change " + change.EntityType.Name,
                IChange change => change.GetType().Name.Humanize()
            },
            { Count: var count } => $"{count} changes"
        };
    }

    public record Activity(
        Guid CommitId,
        DateTimeOffset Timestamp,
        List<ChangeEntity<IChange>> Changes)
    {
        public string ChangeName => ChangeName(Changes);
    }
}
