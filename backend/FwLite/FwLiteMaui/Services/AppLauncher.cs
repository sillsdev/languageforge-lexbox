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
    public Task<bool> OpenInFieldWorks(Guid entryId, string projectName)
    {
        if (fwLinker is null) throw new InvalidOperationException("FwLinker is not available.");
        var link = fwLinker.GetLinkToEntry(entryId, projectName);
        if (link is null) return Task.FromResult(false);
        return _launcher.TryOpenAsync(link);
    }
}
