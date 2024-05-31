using Crdt.Core;
using LexBoxApi.Auth.Attributes;
using LexData;
using LexData.Entities;

namespace LexBoxApi.Services;

public static class CrdtSyncRoutes
{
    public static IEndpointConventionBuilder MapSyncApi(this IEndpointRouteBuilder endpoints,
        string path = "/api/sync/{id}")
    {
        //todo determine if the user has permission to access the project, for now lock down to admin only
        var group = endpoints.MapGroup(path).RequireAuthorization(new AdminRequiredAttribute());
        group.MapGet("/get",
            async (Guid id, LexBoxDbContext dbContext) =>
            {
                return await dbContext.Set<ServerCommit>().Where(c => c.ProjectId == id).GetSyncState();
            });
        group.MapPost("/add",
            async (Guid id, ServerCommit[] commits, LexBoxDbContext dbContext) =>
            {
                foreach (var commit in commits)
                {
                    commit.ProjectId = id;
                    dbContext.Add(commit);//todo should only add if not exists, based on commit id
                }

                await dbContext.SaveChangesAsync();
            });
        group.MapPost("/changes",
            async (Guid id, SyncState clientHeads, LexBoxDbContext dbContext) =>
            {
                var commits = dbContext.Set<ServerCommit>().Where(c => c.ProjectId == id);
                return await commits.GetChanges<ServerCommit, ServerJsonChange>(clientHeads);
            });

        return group;
    }
}
