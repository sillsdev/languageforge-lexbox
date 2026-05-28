using LexCore.ServiceInterfaces;
using Microsoft.AspNetCore.SignalR;
using MiniLcm.Push;

namespace LexBoxApi.Hub;

public class CrdtProjectChangeHub(IPermissionService permissionService) : Hub<IProjectChangeHubClient>, IProjectChangeHubServer
{
    public static string ProjectGroup(Guid projectId) => $"project-{projectId}";

    public async Task ListenForProjectChanges(Guid projectId)
    {
        await permissionService.AssertCanDownloadProject(projectId);
        await Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(projectId));
    }
}
