﻿#if !DISABLE_FW_BRIDGE
using FwDataMiniLcmBridge;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MiniLcm;
using SIL.LCModel;
using SystemTextJsonPatch;

namespace LocalWebApp.Hubs;

public class FwDataMiniLcmHub([FromKeyedServices(FwDataBridgeKernel.FwDataApiKey)] ILexboxApi lexboxApi, FwDataFactory fwDataFactory,
    FwDataProjectContext context) : MiniLcmApiHubBase(lexboxApi)
{
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
        //todo if multiple clients are connected, this will close the project for all of them.
        fwDataFactory.CloseCurrentProject();
        var project = context.Project;
        if (project is null)
        {
            throw new InvalidOperationException("No project is set in the context.");
        }

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
#endif
