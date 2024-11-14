﻿using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Db;
using SIL.Harmony.Entities;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using LocalWebApp.Hubs;
using Microsoft.OpenApi.Models;

namespace LocalWebApp.Routes;

public static class HistoryRoutes
{
    public static IEndpointConventionBuilder MapHistoryRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/history/{project}").WithOpenApi(operation =>
        {
            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = CrdtMiniLcmApiHub.ProjectRouteKey, In = ParameterLocation.Path, Required = true
            });
            return operation;
        });
        group.MapGet("/snapshot/{snapshotId:guid}",
            async (Guid snapshotId, ICrdtDbContext dbcontext) =>
            {
                return await dbcontext.Snapshots.Where(s => s.Id == snapshotId).SingleOrDefaultAsync();
            });
        group.MapGet("/snapshot/at/{timestamp}",
            async (DateTime timestamp, Guid entityId, DataModel dataModel) =>
            {
                //todo requires the timestamp to be exact, otherwise the change made on that timestamp will not be included
                //consider using a commitId and looking up the timestamp, but then we should be exact to the commit which we aren't right now.
                return await dataModel.GetAtTime<IObjectBase>(new DateTimeOffset(timestamp), entityId);
            });
        group.MapGet("/{entityId}",
            (Guid entityId, ICrdtDbContext dbcontext) =>
            {
                var query = from commit in dbcontext.Commits.DefaultOrder()
                    from snapshot in dbcontext.Snapshots.LeftJoin(
                        s => s.CommitId == commit.Id && s.EntityId == entityId)
                    from change in dbcontext.Set<ChangeEntity<IChange>>().LeftJoin(c =>
                        c.CommitId == commit.Id && c.EntityId == entityId)
                    where snapshot.Id != null || change.EntityId != null
                    select new HistoryLineItem(commit.Id,
                        entityId,
                        commit.HybridDateTime.DateTime,
                        snapshot.Id,
                        change.Change,
                        snapshot.Entity,
                        snapshot.TypeName);
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
            IObjectBase? entity,
            string typeName) : this(commitId,
            entityId,
            new DateTimeOffset(timestamp.Ticks,
                TimeSpan.Zero), //todo this is a workaround for linq2db bug where it reads a date and assumes it's local when it's UTC
            snapshotId,
            change?.GetType().Name,
            entity,
            typeName)
        {
        }
    }
}
