using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MiniLcm;
using MiniLcm.Normalization;
using MiniLcm.Validators;
using SIL.LCModel;
using SystemTextJsonPatch;

namespace FwLiteWeb.Hubs;

public class FwDataMiniLcmHub(
    [FromKeyedServices(FwDataBridgeKernel.FwDataApiKey)]
    IMiniLcmApi miniLcmApi,
    FwDataFactory fwDataFactory,
    FwDataProjectContext context,
    MiniLcmApiValidationWrapperFactory validationWrapperFactory
) : MiniLcmApiHubBase(miniLcmApi, validationWrapperFactory, skipWriteNormalization: true)
{
    // Note: FwData already handles string normalization internally (via LCModel), 
    // so we skip the write normalization wrapper for FwData APIs.
    // See: https://github.com/sillsdev/languageforge-lexbox/issues/[issue-number]
    
    public const string ProjectRouteKey = "fwdata";
    public override async Task OnConnectedAsync()
    {
        var project = context.Project;
        if (project is null)
        {
            throw new InvalidOperationException("No project is set in the context.");
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, project.Name);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var project = context.Project;
        if (project is null)
        {
            throw new InvalidOperationException("No project is set in the context.");
        }
        //todo if multiple clients are connected, this will close the project for all of them.
        fwDataFactory.CloseProject(project);

        if (exception is LcmFileLockedException)
        {
            await Clients.Group(project.Name).OnProjectClosed(CloseReason.Locked);
        }
        else
        {
            await Clients.OthersInGroup(project.Name).OnProjectClosed(CloseReason.User);
        }
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, project.Name);
    }
}
