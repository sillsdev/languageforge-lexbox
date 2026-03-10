using FwDataMiniLcmBridge;
using MiniLcm;
using MiniLcm.Normalization;
using MiniLcm.Validators;
using SIL.LCModel;

namespace FwLiteWeb.Hubs;

public class FwDataMiniLcmHub(
    [FromKeyedServices(FwDataBridgeKernel.FwDataApiKey)]
    IMiniLcmApi miniLcmApi,
    FwDataFactory fwDataFactory,
    FwDataProjectContext context,
    MiniLcmApiValidationWrapperFactory validationWrapperFactory,
    MiniLcmApiStringNormalizationWrapperFactory readNormalizationWrapperFactory
)
// Note: FwData already handles string normalization internally (via LCModel),
// so we skip the write normalization wrapper for FwData APIs.
: MiniLcmApiHubBase(miniLcmApi, validationWrapperFactory, readNormalizationWrapperFactory, null, context.Project)
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
