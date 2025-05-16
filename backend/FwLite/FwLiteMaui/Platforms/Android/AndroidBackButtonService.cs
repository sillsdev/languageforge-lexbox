using FwLiteShared.Services;
using Microsoft.JSInterop;

namespace FwLiteMaui;

public class AndroidBackButtonService(IServiceProvider services) : IMultiWindowService
{
    [JSInvokable]
    public void OpenNewWindow(string? url = null, int? width = null)
    {
        var mainPage = services.GetRequiredService<MainPage>();
        mainPage.BackButtonPressed +=
    }
}
