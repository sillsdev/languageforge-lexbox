using Microsoft.AspNetCore.SignalR;
#if !DISABLE_FW_BRIDGE
using SIL.LCModel;
#endif

namespace LocalWebApp.Hubs;

public class LockedProjectFilter: IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (
            #if !DISABLE_FW_BRIDGE
            LcmFileLockedException
            #else
            IOException
            #endif
            )
        {
            await TypedHubHelper<ILexboxHubClient>.TypeClients(invocationContext.Hub.Clients)
                .Caller.OnProjectClosed(CloseReason.Locked);
            throw new HubException("The project is locked.");
        }
    }

    private class TypedHubHelper<TClient> : Hub<TClient> where TClient : class
    {
        public TypedHubHelper(IHubCallerClients clients) : base()
        {
            ((Hub)this).Clients = clients;
        }

        public static IHubCallerClients<TClient> TypeClients(IHubCallerClients clients)
        {
            return new TypedHubHelper<TClient>(clients).Clients;
        }
    }
}
