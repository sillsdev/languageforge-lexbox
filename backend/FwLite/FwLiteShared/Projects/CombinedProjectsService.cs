using FwLiteShared.Auth;
using FwLiteShared.Sync;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MiniLcm.Models;
using MiniLcm.Project;

namespace FwLiteShared.Projects;

public record ProjectModel(
    string Name,
    bool Crdt,
    bool Fwdata,
    bool Lexbox = false,
    LexboxServer? Server = null,
    Guid? Id = null);

public record ServerProjects(LexboxServer Server, ProjectModel[] Projects);
public class CombinedProjectsService(LexboxProjectService lexboxProjectService,
    CrdtProjectsService crdtProjectsService,
    IEnumerable<IProjectProvider> projectProviders,
    OAuthClientFactory oAuthClientFactory)
{
    private IProjectProvider? FwDataProjectProvider => projectProviders.FirstOrDefault(p => p.DataFormat == ProjectDataFormat.FwData);
    [JSInvokable]
    public bool SupportsFwData() => FwDataProjectProvider is not null;
    [JSInvokable]
    public async Task<ServerProjects[]> RemoteProjects(bool forceRefresh)
    {
        var lexboxServers = lexboxProjectService.Servers();
        ServerProjects[] serverProjects = new ServerProjects[lexboxServers.Length];
        for (var i = 0; i < lexboxServers.Length; i++)
        {
            var server = lexboxServers[i];
            var projectModels = await ServerProjects(server, forceRefresh);
            serverProjects[i] = new ServerProjects(server, projectModels);
        }

        return serverProjects;
    }

    private async Task<ProjectModel[]> ServerProjects(LexboxServer server, bool forceRefresh)
    {
        if (forceRefresh) lexboxProjectService.InvalidateProjectsCache(server);
        var lexboxProjects = await lexboxProjectService.GetLexboxProjects(server);
        var projectModels = lexboxProjects.Select(p => new ProjectModel(p.Name,
                Crdt: p.IsCrdtProject,
                Fwdata: false,
                Lexbox: true,
                server,
                p.Id))
            .ToArray();
        return projectModels;
    }

    [JSInvokable]
    public async Task<ProjectModel[]> ServerProjects(string serverId, bool forceRefresh)
    {
        var server = lexboxProjectService.Servers().FirstOrDefault(s => s.Id == serverId);
        if (server is null) return [];
        return await ServerProjects(server, forceRefresh);
    }

    [JSInvokable]
    public async ValueTask<IReadOnlyCollection<ProjectModel>> LocalProjects()
    {
        await crdtProjectsService.EnsureProjectDataCacheIsLoaded();
        var crdtProjects = crdtProjectsService.ListProjects();
        //todo get project Id and use that to specify the Id in the model. Also pull out server
        var projects = crdtProjects.ToDictionary(p => p.Name,
            p => new ProjectModel(p.Name,
                true,
                false,
                p.Data?.OriginDomain is not null,
                lexboxProjectService.GetServer(p.Data),
                p.Data?.Id));
        //basically populate projects and indicate if they are lexbox or fwdata
        if (FwDataProjectProvider is not null)
        {
            foreach (var p in FwDataProjectProvider.ListProjects())
            {
                if (projects.TryGetValue(p.Name, out var project))
                {
                    projects[p.Name] = project with { Fwdata = true };
                }
                else
                {
                    projects.Add(p.Name, new ProjectModel(p.Name, false, true));
                }
            }
        }


        return projects.Values;
    }

    [JSInvokable]
    public async Task DownloadProject(Guid lexboxProjectId, string projectName, LexboxServer server)
    {
        var currentUser = await oAuthClientFactory.GetClient(server).GetCurrentUser();
        await crdtProjectsService.CreateProject(new(projectName,
            lexboxProjectId,
            server.Authority,
            async (provider, project) =>
            {
                await provider.GetRequiredService<SyncService>().ExecuteSync(true);
            },
            SeedNewProjectData: false,
            AuthenticatedUser: currentUser?.Name,
            AuthenticatedUserId: currentUser?.Id));
    }

    [JSInvokable]
    public async Task CreateProject(string name)
    {
        await crdtProjectsService.CreateExampleProject(name);
    }
}
