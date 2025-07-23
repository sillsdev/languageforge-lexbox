using System.Text.Json.Serialization;
using FwLiteShared.Auth;
using FwLiteShared.Sync;
using LcmCrdt;
using LexCore.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MiniLcm.Models;
using MiniLcm.Project;

namespace FwLiteShared.Projects;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DownloadProjectByCodeResult
{
    Success,
    NotCrdtProject,
    ProjectNotFound,
    ProjectAlreadyDownloaded,
}

public record ProjectModel(
    string Name,
    string Code,
    bool Crdt,
    bool Fwdata,
    bool Lexbox = false,
    ProjectRole Role = ProjectRole.Editor,
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

public record ServerProjects(LexboxServer Server, ProjectModel[] Projects, bool CanDownloadByCode);
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
            serverProjects[i] = await ServerProjects(lexboxServers[i]);
        }

        return serverProjects;
    }

    private async Task<ServerProjects> ServerProjects(LexboxServer server, bool forceRefresh = false)
    {
        if (forceRefresh) lexboxProjectService.InvalidateProjectsCache(server);
        var lexboxProjects = await lexboxProjectService.GetLexboxProjects(server);
        var user = await lexboxProjectService.GetLexboxUser(server);
        await UpdateProjectServerInfo(lexboxProjects.Projects, user);
        var projectModels = lexboxProjects.Projects.Select(p => new ProjectModel(
                p.Name,
                p.Code,
                Crdt: p.IsCrdtProject,
                Fwdata: false,
                Lexbox: true,
                Role: p.Role,
                server,
                p.Id))
            .ToArray();
        return new(server, projectModels, lexboxProjects.CanDownloadByCode);
    }

    private async Task UpdateProjectServerInfo(FieldWorksLiteProject[] lexboxProjects, LexboxUser? lexboxUser)
    {
        foreach (var serverProject in lexboxProjects)
        {
            var localProject = crdtProjectsService.GetProject(serverProject.Id);
            if (localProject?.Data is null) continue;
            await crdtProjectsService.UpdateProjectServerInfo(localProject, lexboxUser?.Name, lexboxUser?.Id, ToRole(serverProject.Role));
        }
    }


    [JSInvokable]
    public async Task<ServerProjects?> ServerProjects(string serverId, bool forceRefresh)
    {
        var server = lexboxProjectService.Servers().FirstOrDefault(s => s.Id == serverId);
        if (server is null) return null;
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
                p.Data is null ? ProjectRole.Unknown : FromRole(p.Data.Role),
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

    private UserProjectRole ToRole(ProjectRole role) =>
        role switch
        {
            ProjectRole.Observer => UserProjectRole.Observer,
            ProjectRole.Editor => UserProjectRole.Editor,
            ProjectRole.Manager => UserProjectRole.Manager,
            _ => UserProjectRole.Unknown
        };

    private ProjectRole FromRole(UserProjectRole role) =>
        role switch
        {
            UserProjectRole.Observer => ProjectRole.Observer,
            UserProjectRole.Editor => ProjectRole.Editor,
            UserProjectRole.Manager => ProjectRole.Manager,
            _ => ProjectRole.Unknown
        };

    [JSInvokable]
    public async Task<DownloadProjectByCodeResult> DownloadProjectByCode(string code, LexboxServer server, UserProjectRole? userRole = null)
    {
        var serverProjects = await ServerProjects(server, false);
        var project = serverProjects.Projects.FirstOrDefault(p => p.Code == code);
        if (project is null)
        {
            if (serverProjects.CanDownloadByCode)
            {
                var projectId = await lexboxProjectService.GetLexboxProjectId(server, code);
                if (projectId is null) return DownloadProjectByCodeResult.ProjectNotFound;
                var isCrdtProject = await lexboxProjectService.IsCrdtProject(server, code);
                if (!isCrdtProject) return DownloadProjectByCodeResult.NotCrdtProject;
                if (crdtProjectsService.ProjectExists(code)) return DownloadProjectByCodeResult.ProjectAlreadyDownloaded;
                var role = userRole.HasValue ? FromRole(userRole.Value) : ProjectRole.Editor;
                project = new ProjectModel(
                    Name: code,
                    Code: code,
                    Crdt: isCrdtProject,
                    Fwdata: false,
                    Lexbox: true,
                    Role: role,
                    Server: server,
                    Id: projectId
                );
                await DownloadProject(project);
                return DownloadProjectByCodeResult.Success;
            }
            return DownloadProjectByCodeResult.ProjectNotFound;
        }
        await DownloadProject(project);
        return DownloadProjectByCodeResult.Success;
    }

    [JSInvokable]
    public async Task DownloadProject(ProjectModel project)
    {
        var server = project.Server ?? throw new ArgumentNullException($"{nameof(project.Server)} is null for project {project.Code}");
        var projectId = project.Id ?? throw new ArgumentNullException($"{nameof(project.Id)} is null for project {project.Code}");
        var currentUser = await oAuthClientFactory.GetClient(server).GetCurrentUser();
        await Task.Run(async () => await crdtProjectsService.CreateProject(new(project.Name,
            project.Code,
            projectId,
            server.Authority,
            async (provider, project) =>
            {
                await provider.GetRequiredService<SyncService>().ExecuteSync(true);
            },
            SeedNewProjectData: false,
            AuthenticatedUser: currentUser?.Name,
            AuthenticatedUserId: currentUser?.Id,
            Role: ToRole(project.Role))));
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
