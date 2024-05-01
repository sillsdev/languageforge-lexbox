using CrdtLib;
using CrdtLib.Db;
using LcmCrdt;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LexBoxApi.Services;

public static class CrdtSyncApi
{
    public static IEndpointConventionBuilder MapSyncApi(this IEndpointRouteBuilder endpoints,
        string path = "/api/sync/{id}")
    {
        var group = endpoints.MapGroup(path);
        group.MapGet("/get",
            async (Guid id, DataModelProvider<Guid> provider) =>
            {
                var dataModel = await provider.GetDataModel(id);
                return await dataModel.GetSyncState();
            });
        group.MapPost("/add",
            async (Guid id, DataModelProvider<Guid> provider, Commit[] commits) =>
            {
                var dataModel = await provider.GetDataModel(id);
                await ((ISyncable)dataModel).AddRangeFromSync(commits);
            });
        group.MapPost("/changes",
            async (Guid id, DataModelProvider<Guid> provider, SyncState clientHeads) =>
            {
                ArgumentNullException.ThrowIfNull(clientHeads);
                var dataModel = await provider.GetDataModel(id);
                return await dataModel.GetChanges(clientHeads);
            });

        return group;
    }

    public class LexboxProjectProvider(ProjectsService projectsService) : IProjectProvider<Guid>
    {
        public async ValueTask<CrdtProject> GetProject(Guid id)
        {
            //todo get project by identifier
            var project = projectsService.GetProject("Sena 3");
            if (project is null) project = await projectsService.CreateProject("Sena 3", id, null);
            return project;
        }
    }
}
