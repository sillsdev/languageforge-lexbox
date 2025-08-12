#if INCLUDE_FWDATA_BRIDGE
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
#endif
using FwLiteShared.Services;
using Microsoft.JSInterop;

namespace FwLiteMaui.Services;

public class AppLauncher
#if INCLUDE_FWDATA_BRIDGE
(FwDataFactory fwDataFactory, FieldWorksProjectList projectList)
#endif
: IAppLauncher

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

#if INCLUDE_FWDATA_BRIDGE
    [JSInvokable]
    public Task<bool> OpenInFieldWorks(Guid entryId, string projectName)
    {
        var project = projectList.GetProject(projectName);
        if (project is null) return Task.FromResult(false);
        fwDataFactory.CloseProject(project);
        return _launcher.TryOpenAsync(FwLink.ToEntry(entryId, projectName));
    }
#endif
}
