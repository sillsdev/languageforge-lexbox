using FwLiteShared.Services;
using Microsoft.JSInterop;

namespace FwLiteMaui;

public class WindowsMultiWindowService(IApplication app, IServiceProvider services) : IMultiWindowService
{
    [JSInvokable]
    public void OpenNewWindow(string? url = null, int? width = null)
    {
        var mainPage = services.GetRequiredService<MainPage>();
        if (url is not null) mainPage.StartPath = url;
        app.OpenWindow(App.CreateWindow(mainPage, width));
    }
}
