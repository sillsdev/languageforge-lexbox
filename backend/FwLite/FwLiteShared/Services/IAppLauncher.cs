using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public interface IAppLauncher
{
    [JSInvokable]
    Task<bool> CanOpen(string uri);

    [JSInvokable]
    Task Open(string uri);

    [JSInvokable]
    Task<bool> TryOpen(string uri);
    [JSInvokable]
    Task<bool> OpenInFieldWorks(Guid entryId, string projectName);
}
