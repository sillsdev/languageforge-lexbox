using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public interface IMultiWindowService
{
    [JSInvokable]
    void OpenNewWindow(string? url = null, int? width = null);
}
