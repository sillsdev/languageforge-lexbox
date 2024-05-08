using Crdt;
using Crdt.Changes;
using Crdt.Core;
using Crdt.Db;
using Crdt.Entities;
using Crdt.Helpers;
using LcmCrdt;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;

namespace LocalWebApp;

public static class HistoryRoutes
{
    public static IEndpointConventionBuilder MapHistoryRoutes(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var projectName = context.GetProjectName();
            if (!string.IsNullOrWhiteSpace(projectName))
            {
                var projectsService = context.RequestServices.GetRequiredService<ProjectsService>();
                projectsService.SetProjectScope(projectsService.GetProject(projectName) ??
                                                throw new InvalidOperationException($"Project {projectName} not found"));
            }

            await next(context);
        });
        var group = app.MapGroup("/api/history/{project}").WithOpenApi();
        group.MapGet("/snapshot/{snapshotId}",
            async (Guid snapshotId, CrdtDbContext dbcontext) =>
            {
                return await dbcontext.Snapshots.Where(s => s.Id == snapshotId).SingleOrDefaultAsync();
            });
        group.MapGet("/{entityId}",
            (Guid entityId, CrdtDbContext dbcontext) =>
            {
                var query = from commit in dbcontext.Commits.DefaultOrder()
                    from snapshot in dbcontext.Snapshots.LeftJoin(s => s.CommitId == commit.Id && s.EntityId == entityId)
                    from change in dbcontext.ChangeEntities.LeftJoin(c => c.CommitId == commit.Id && c.EntityId == entityId)
                    where snapshot.Id != null || change.EntityId != null
                    select new HistoryLineItem(entityId,
                        commit.HybridDateTime.DateTime,
                        snapshot.Id,
                        change.Change,
                        snapshot.Entity);
                return query.ToLinqToDB().AsAsyncEnumerable();
            });
        return group;
    }

    public record HistoryLineItem(
        Guid EntityId,
        DateTimeOffset Timestamp,
        Guid? SnapshotId,
        string? ChangeName,
        IObjectBase? Entity,
        string? EntityName)
    {
        public HistoryLineItem(
            Guid entityId,
            DateTimeOffset timestamp,
            Guid? snapshotId,
            IChange? change,
            IObjectBase? entity) : this(entityId,
            timestamp,
            snapshotId,
            change?.GetType().Name,
            entity,
            entity?.TypeName)
        {
        }
    }
}
