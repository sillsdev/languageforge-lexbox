using MiniLcm;
using MiniLcm.Models;

namespace FwLiteShared.Services;

public partial class MiniLcmApiNotifyWrapper: IMiniLcmReadApi, IMiniLcmWriteApi, IDisposable
{
    [BeaKona.AutoInterface]
    private readonly IMiniLcmReadApi wrappedReadApi;

    [BeaKona.AutoInterface]
    private readonly IMiniLcmWriteApi wrappedWriteApi;

    public MiniLcmApiNotifyWrapper(IMiniLcmApi api)
    {
        wrappedReadApi = api;
        wrappedWriteApi = api;
    }

    public void Dispose()
    {
        if (wrappedReadApi is IDisposable api) api.Dispose();
        // No need to call wrappedWriteApi.Dispose() as they're guaranteed to be the same object
    }
}
