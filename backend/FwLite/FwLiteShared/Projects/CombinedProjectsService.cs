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
    string Code,
    bool Crdt,
    bool Fwdata,
    bool Lexbox = false,
    LexboxServer? Server = null,
    Guid? Id = null)
{
    public string? ApiEndpoint =>
        (this) switch
        {
            { Crdt: true } => $"/api/mini-lcm/{ProjectDataFormat.Harmony}/{Code}",
            { Fwdata: true } => $"/api/mini-lcm/{ProjectDataFormat.FwData}/{Code}",
            _ => null
        };
}

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
    public async Task<ServerProjects[]> RemoteProjects()
    {
        var lexboxServers = lexboxProjectService.Servers();
        ServerProjects[] serverProjects = new ServerProjects[lexboxServers.Length];
        for (var i = 0; i < lexboxServers.Length; i++)
        {
            var server = lexboxServers[i];
            var projectModels = await ServerProjects(server);
            serverProjects[i] = new ServerProjects(server, projectModels);
        }

        return serverProjects;
    }

    private async Task<ProjectModel[]> ServerProjects(LexboxServer server, bool forceRefresh = false)
    {
        if (forceRefresh) lexboxProjectService.InvalidateProjectsCache(server);
        var lexboxProjects = await lexboxProjectService.GetLexboxProjects(server);
        var projectModels = lexboxProjects.Select(p => new ProjectModel(
                p.Name,
                p.Code,
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
        var projects = crdtProjects.ToDictionary(p => p.Name, // actually the code
            p => new ProjectModel(
                p.Data?.Name ?? throw new NullReferenceException($"Project Data/Name is null for project {p.Name}"),
                p.Name,
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
                    projects.Add(p.Name, new ProjectModel(p.Name, p.Name, false, true));
                }
            }
        }


        return projects.Values;
    }

    public async Task DownloadProject(string code, LexboxServer server)
    {
        var serverProjects = await ServerProjects(server, false);
        var project = serverProjects.FirstOrDefault(p => p.Code == code)
            ?? throw new InvalidOperationException($"Project {code} not found on server {server.Authority}");
        await DownloadProject(project);
    }

    [JSInvokable]
    public async Task DownloadProject(ProjectModel project)
    {
        var server = project.Server ?? throw new ArgumentNullException($"{nameof(project.Server)} is null for project {project.Code}");
        var projectId = project.Id ?? throw new ArgumentNullException($"{nameof(project.Id)} is null for project {project.Code}");
        var currentUser = await oAuthClientFactory.GetClient(server).GetCurrentUser();
        await crdtProjectsService.CreateProject(new(project.Name,
            project.Code,
            projectId,
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

    [JSInvokable]
    public async Task DeleteProject(string code)
    {
        await crdtProjectsService.DeleteProject(code);
    }
}
