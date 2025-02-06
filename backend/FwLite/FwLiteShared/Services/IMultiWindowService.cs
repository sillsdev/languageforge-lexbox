using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public interface IMultiWindowService
{
    [JSInvokable]
    public ValueTask OpenNewWindow(string? url = null);
}
