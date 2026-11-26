using System.Text.Json.Serialization;
using FwLiteShared.Auth;
using FwLiteShared.Events;
using FwLiteShared.KeepAwake;
using FwLiteShared.LocalSync;
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
    Forbidden,
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
    OAuthClientFactory oAuthClientFactory,
    FolderProjectOpener folderProjectOpener,
    IKeepAwake keepAwake,
    ProjectEventBus projectEventBus,
    IServiceProvider serviceProvider)
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
        if (forceRefresh)
            lexboxProjectService.InvalidateProjectsCache(server);
        var lexboxProjects = await lexboxProjectService.GetLexboxProjects(server);
        var user = await lexboxProjectService.GetLexboxUser(server);
        // Can migrate a project's db and replay its whole history, so keep it off the UI thread
        await Task.Run(async () => await UpdateProjectServerInfo(lexboxProjects.Projects, user));
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
            // A local-only project with the same id (e.g. before it's uploaded) isn't bound to the server, so its author and role stay local
            if (localProject?.Data?.OriginDomain is null) continue;
            var role = serverProject.Role.ToUserProjectRole();
            if (await crdtProjectsService.UpdateProjectServerInfo(localProject, lexboxUser?.Name, lexboxUser?.Id, role))
                projectEventBus.PublishEvent(localProject, new ProjectRoleChangedEvent(role));
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
    public IReadOnlyCollection<ProjectModel> LocalProjects()
    {
        var crdtProjects = crdtProjectsService.ListProjects();
        //todo get project Id and use that to specify the Id in the model. Also pull out server
        var projects = crdtProjects.ToDictionary(p => p.Name, // actually the code
            p => new ProjectModel(
                p.Data?.Name ?? p.Name,
                p.Name,
                true,
                false,
                p.Data?.OriginDomain is not null,
                p.Data is null ? ProjectRole.Unknown : p.Data.Role.ToProjectRole(),
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

    [JSInvokable]
    public async Task<DownloadProjectByCodeResult> DownloadProjectByCode(string code, LexboxServer server, UserProjectRole? userRole = null)
    {
        // Note: the local project might be from a different server, but that's currently only relevant
        // for devs and we still can't download two projects with the same code
        if (crdtProjectsService.ProjectExists(code)) return DownloadProjectByCodeResult.ProjectAlreadyDownloaded;
        var serverProjects = await ServerProjects(server, false);
        var project = serverProjects.Projects.FirstOrDefault(p => p.Code == code);
        if (project is null)
        {
            if (serverProjects.CanDownloadByCode)
            {
                var (status, projectId) = await lexboxProjectService.GetLexboxProjectId(server, code);
                if (status != DownloadProjectByCodeResult.Success) return status;
                var role = userRole?.ToProjectRole() ?? ProjectRole.Editor;
                project = new ProjectModel(
                    Name: code,
                    Code: code,
                    Crdt: true,
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
        var server = project.Server ?? throw new ArgumentNullException($"{nameof(project.Server)} is null for project '{project.Code}'");
        var projectId = project.Id ?? throw new ArgumentNullException($"{nameof(project.Id)} is null for project '{project.Code}'");
        var currentUser = await oAuthClientFactory.GetClient(server).GetCurrentUser();
        await keepAwake.RunAsync(
            new($"Downloading project {project.Name}"),
            async () => await Task.Run(async () => await crdtProjectsService.CreateProject(new(project.Name,
                project.Code,
                projectId,
                server.Authority,
                async (provider, project) =>
                {
                    await provider.GetRequiredService<SyncService>().ExecuteSync(true);
                },
                AuthenticatedUser: currentUser?.Name,
                AuthenticatedUserId: currentUser?.Id,
                Role: project.Role.ToUserProjectRole()))));
    }

    /// <summary>Downloads the project in a sync folder, unless this device already has it.</summary>
    [JSInvokable]
    public async Task<OpenFromFolderResult> OpenFromFolder(string path)
    {
        return await keepAwake.RunAsync(new("Opening a project from a folder"),
            async () => await Task.Run(async () => await folderProjectOpener.OpenFromFolder(path)));
    }

    [JSInvokable]
    public async Task<FolderSyncSummary> SyncProjectWithFolder(string projectCode, string path)
    {
        var result = await keepAwake.RunAsync(new("Syncing with a folder"),
            async () => await Task.Run(async () => await folderProjectOpener.SyncWithFolder(projectCode, path)));
        return FolderSyncSummary.From(result);
    }

    /// <param name="authorServer">Credit edits to the account logged in to this server, else to the known Lexbox account</param>
    [JSInvokable]
    public Task CreateProject(string name, string code, WritingSystemId vernacularWs, LexboxServer? authorServer, WritingSystemId? analysisWs = null)
    {
        return Task.Run(async () =>
        {
            var author = authorServer is null ? (await FindLexboxAccount())?.User
                : await oAuthClientFactory.GetClient(authorServer).GetCachedUser()
                    ?? throw new InvalidOperationException($"Not logged in to {authorServer.DisplayName}");
            await crdtProjectsService.CreateProjectFromTemplate(new(name, code, Role: UserProjectRole.Manager,
                AuthenticatedUser: author?.Name, AuthenticatedUserId: author?.Id), vernacularWs, analysisWs);
        });
    }

    /// <summary>
    /// New projects are credited to the user's Lexbox account whenever we know it, so the device-wide local name
    /// is only for people who have never used Lexbox here.
    /// </summary>
    [JSInvokable]
    public async Task<ServerStatus?> KnownLexboxAccount()
    {
        if (await FindLexboxAccount() is not { } account) return null;
        return new ServerStatus(account.Server.DisplayName, account.LoggedIn, account.User.Name, account.Server);
    }

    // A login wins over a project's remembered user, which may be stale
    private async Task<(LexboxServer Server, LexboxUser User, bool LoggedIn)?> FindLexboxAccount()
    {
        var servers = lexboxProjectService.Servers();
        foreach (var server in servers)
        {
            if (await oAuthClientFactory.GetClient(server).GetCachedUser() is { } user) return (server, user, true);
        }
        // only projects that sync with a server: the example project has a made-up user
        foreach (var data in crdtProjectsService.ListProjects().Select(p => p.Data))
        {
            if (data?.OriginDomain is null || string.IsNullOrEmpty(data.LastUserId) || string.IsNullOrEmpty(data.LastUserName)) continue;
            var origin = new Uri(data.OriginDomain);
            // the server may no longer be configured, but the account is still theirs
            var server = servers.FirstOrDefault(s => s.Authority.Authority == origin.Authority) ?? new LexboxServer(origin, origin.Host);
            return (server, new LexboxUser(data.LastUserName, data.LastUserId), false);
        }
        return null;
    }

    [JSInvokable]
    public Task CreateDemoProject(string name)
    {
        return Task.Run(async () =>
        {
            await crdtProjectsService.CreateExampleProject(name);
        });
    }

    [JSInvokable]
    public Task DeleteProject(string code)
    {
        return Task.Run(async () =>
        {
            await crdtProjectsService.DeleteProject(code);
        });
    }

    // Uploads into the Lexbox project with the same id as the local one, so folder-shared copies keep matching
    [JSInvokable]
    public async Task UploadCrdtProject(LexboxServer server, string code)
    {
        var project = crdtProjectsService.GetProject(code)
            ?? throw new InvalidOperationException($"CRDT project '{code}' not found");
        await keepAwake.RunAsync(
            new($"Uploading project {project.Name}"),
            async () => await Task.Run(async () =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var projectData = await scope.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContext(project);
                // A failed upload clears the origin, which would unbind a project that already syncs
                if (projectData.OriginDomain is not null)
                    throw new InvalidOperationException($"Project '{code}' already syncs with a server");
                await scope.ServiceProvider.GetRequiredService<SyncService>().UploadProject(projectData.Id, server);
            }));
    }
}
