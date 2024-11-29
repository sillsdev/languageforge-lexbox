﻿using FwDataMiniLcmBridge;
using FwLiteShared.Auth;
using FwLiteShared.Sync;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;

namespace FwLiteShared.Projects;

public record ProjectModel(
    string Name,
    bool Crdt,
    bool Fwdata,
    bool Lexbox = false,
    string? ServerAuthority = null,
    Guid? Id = null);

public record ServerProjects(LexboxServer Server, ProjectModel[] Projects);
public class CombinedProjectsService(LexboxProjectService lexboxProjectService, CrdtProjectsService crdtProjectsService,
    FieldWorksProjectList fieldWorksProjectList)
{
    public async Task<ServerProjects[]> RemoteProjects()
    {
        var lexboxServers = lexboxProjectService.Servers();
        ServerProjects[] serverProjects = new ServerProjects[lexboxServers.Length];
        for (var i = 0; i < lexboxServers.Length; i++)
        {
            var server = lexboxServers[i];
            var lexboxProjects = await lexboxProjectService.GetLexboxProjects(server);
            serverProjects[i] = new ServerProjects(server,
                lexboxProjects.Select(p => new ProjectModel(p.Name,
                        Crdt: p.IsCrdtProject,
                        Fwdata: false,
                        Lexbox: true,
                        server.Authority.Authority,
                        p.Id))
                    .ToArray());
        }

        return serverProjects;
    }

    public async Task<IReadOnlyCollection<ProjectModel>> LocalProjects()
    {
        var crdtProjects = await crdtProjectsService.ListProjects();
        //todo get project Id and use that to specify the Id in the model. Also pull out server
        var projects = crdtProjects.ToDictionary(p => p.Name,
            p =>
            {
                var uri = p.Data?.OriginDomain is not null ? new Uri(p.Data.OriginDomain) : null;
                return new ProjectModel(p.Name,
                    true,
                    false,
                    p.Data?.OriginDomain is not null,
                    uri?.Authority,
                    p.Data?.Id);
            });
        //basically populate projects and indicate if they are lexbox or fwdata
        foreach (var p in fieldWorksProjectList.EnumerateProjects())
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

        return projects.Values;
    }

    public async Task DownloadProject(Guid lexboxProjectId, string projectName, LexboxServer server)
    {
        await crdtProjectsService.CreateProject(new(projectName,
            lexboxProjectId,
            server.Authority,
            async (provider, project) =>
            {
                await provider.GetRequiredService<SyncService>().ExecuteSync();
            },
            SeedNewProjectData: false));
    }
}