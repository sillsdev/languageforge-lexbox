using LexBoxApi.Auth;
using LexCore.ServiceInterfaces;
using Microsoft.AspNetCore.SignalR;
using MiniLcm.Push;

namespace LexBoxApi.Hub;

public class CrdtProjectChangeHub(LoggedInContext loggedInContext, IPermissionService permissionService) : Hub<IProjectChangeListener>
{
    public static string ProjectGroup(Guid projectId) => $"project-{projectId}";

    public async Task ListenForProjectChanges(Guid projectId)
    {
        await permissionService.AssertCanSyncProject(projectId);
        await Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(projectId));
    }
}
