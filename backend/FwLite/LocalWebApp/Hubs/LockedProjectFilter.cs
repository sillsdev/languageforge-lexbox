using Microsoft.AspNetCore.SignalR;
using SIL.LCModel;

namespace LocalWebApp.Hubs;

public class LockedProjectFilter: IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (LcmFileLockedException)
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
