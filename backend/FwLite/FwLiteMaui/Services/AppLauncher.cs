using FwLiteShared.Services;
using Microsoft.JSInterop;

namespace FwLiteMaui.Services;

public class AppLauncher(IFwLinker? fwLinker = null) : IAppLauncher
{
    private readonly ILauncher _launcher = Launcher.Default;

    [JSInvokable]
    public Task<bool> CanOpen(string uri)
    {
        return _launcher.CanOpenAsync(uri);
    }

    [JSInvokable]
    public Task Open(string uri)
    {
        return _launcher.OpenAsync(uri);
    }

    [JSInvokable]
    public Task<bool> TryOpen(string uri)
    {
        return _launcher.TryOpenAsync(uri);
    }

    [JSInvokable]
    public async Task<bool> OpenInFieldWorks(Guid entryId, string projectName)
    {
        if (fwLinker is null) throw new InvalidOperationException("FwLinker is not available.");
        var link = await fwLinker.GetLinkToEntryAsync(entryId, projectName);
        if (link is null) return false;
        return await _launcher.TryOpenAsync(link);
    }
}
