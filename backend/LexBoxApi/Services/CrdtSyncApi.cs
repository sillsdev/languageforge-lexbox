using CrdtLib;
using CrdtLib.Db;

namespace LexBoxApi.Services;

public static class CrdtSyncApi
{
    public static IEndpointConventionBuilder MapSyncApi(this IEndpointRouteBuilder endpoints, string path = "/api/sync")
    {
        var group = endpoints.MapGroup(path);
        group.MapGet("/get", async (DataModel dataModel) => await dataModel.GetSyncState());
        group.MapPost("/add",
            async (DataModel dataModel, Commit[] commits) => await ((ISyncable)dataModel).AddRangeFromSync(commits));
        group.MapPost("/changes",
            async (DataModel dataModel, SyncState clientHeads) =>
            {
                ArgumentNullException.ThrowIfNull(clientHeads);
                return await dataModel.GetChanges(clientHeads);
            });
        return group;
    }
}
