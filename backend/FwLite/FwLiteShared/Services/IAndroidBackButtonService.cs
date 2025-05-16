using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public delegate bool BackButtonHandler();

public interface IAndroidBackButtonService
{
    [JSInvokable]
    void RegisterBackButtonHandler(BackButtonHandler handler);
}
