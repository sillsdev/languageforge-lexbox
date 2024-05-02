using Crdt.Core;
using LexData;
using LexData.Entities;

namespace LexBoxApi.Services;

public static class CrdtSyncApi
{
    public static IEndpointConventionBuilder MapSyncApi(this IEndpointRouteBuilder endpoints,
        string path = "/api/sync/{id}")
    {
        var group = endpoints.MapGroup(path);
        group.MapGet("/get",
            async (Guid id, LexBoxDbContext dbContext) =>
            {
                return await dbContext.Set<CrdtCommit>().Where(c => c.ProjectId == id).GetSyncState();
            });
        group.MapPost("/add",
            async (Guid id, CrdtCommit[] commits, LexBoxDbContext dbContext) =>
            {
                foreach (var commit in commits)
                {
                    commit.ProjectId = id;
                    dbContext.Add(commit);
                }

                await dbContext.SaveChangesAsync();
            });
        group.MapPost("/changes",
            async (Guid id, SyncState clientHeads, LexBoxDbContext dbContext) =>
            {
                var commits = dbContext.Set<CrdtCommit>().Where(c => c.ProjectId == id);
                return await commits.GetChanges<CrdtCommit, JsonChange>(clientHeads);
            });

        return group;
    }
}
