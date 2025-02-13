#if INCLUDE_FWDATA_BRIDGE
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using FwLiteShared.Services;
using Microsoft.JSInterop;

namespace FwLiteMaui.Services;

public class AppLauncher(FwDataFactory fwDataFactory, FieldWorksProjectList projectList) : IAppLauncher
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
        var project = projectList.GetProject(projectName);
        if (project is null) return Task.FromResult(false);
        fwDataFactory.CloseProject(project);
        return _launcher.TryOpenAsync(FwLink.ToEntry(entryId, projectName));
    }
}
#endif
