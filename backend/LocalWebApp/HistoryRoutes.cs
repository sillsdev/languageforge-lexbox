using System.ComponentModel.DataAnnotations;
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
        group.MapGet("/snapshot/{snapshotId:guid}",
            async (Guid snapshotId, CrdtDbContext dbcontext) =>
            {
                return await dbcontext.Snapshots.Where(s => s.Id == snapshotId).SingleOrDefaultAsync();
            });
        group.MapGet("/snapshot/at/{timestamp}",
            async (DateTime timestamp, Guid entityId, DataModel dataModel) =>
            {
                //todo requires the timestamp to be exact, otherwise the change made on that timestamp will not be included
                //consider using a commitId and looking up the timestamp, but then we should be exact to the commit which we aren't right now.
                return await dataModel.GetEntitySnapshotAtTime(new DateTimeOffset(timestamp), entityId);
            });
        group.MapGet("/{entityId}",
            (Guid entityId, CrdtDbContext dbcontext) =>
            {
                var query = from commit in dbcontext.Commits.DefaultOrder()
                    from snapshot in dbcontext.Snapshots.LeftJoin(s => s.CommitId == commit.Id && s.EntityId == entityId)
                    from change in dbcontext.ChangeEntities.LeftJoin(c => c.CommitId == commit.Id && c.EntityId == entityId)
                    where snapshot.Id != null || change.EntityId != null
                    select new HistoryLineItem(commit.Id,
                        entityId,
                        commit.HybridDateTime.DateTime,
                        snapshot.Id,
                        change.Change,
                        snapshot.Entity);
                return query.ToLinqToDB().AsAsyncEnumerable();
            });
        return group;
    }

    public record HistoryLineItem(
        Guid CommitId,
        Guid EntityId,
        DateTimeOffset Timestamp,
        Guid? SnapshotId,
        string? ChangeName,
        IObjectBase? Entity,
        string? EntityName)
    {
        public HistoryLineItem(
            Guid commitId,
            Guid entityId,
            DateTimeOffset timestamp,
            Guid? snapshotId,
            IChange? change,
            IObjectBase? entity) : this(commitId, entityId,
            new DateTimeOffset(timestamp.Ticks, TimeSpan.Zero),//todo this is a workaround for linq2db bug where it reads a date and assumes it's local when it's UTC
            snapshotId,
            change?.GetType().Name,
            entity,
            entity?.TypeName)
        {
        }
    }
}
