using FwLiteShared.Services;
using Microsoft.JSInterop;

namespace FwLiteMaui;

public class WindowsMultiWindowService(IApplication app, IServiceProvider services) : IMultiWindowService
{
    [JSInvokable]
    public ValueTask OpenNewWindow(string? url = null)
    {
        var mainPage = services.GetRequiredService<MainPage>();
        if (url is not null) mainPage.StartPath = url;
        app.OpenWindow(new Window(mainPage));
        return ValueTask.CompletedTask;
    }
}
