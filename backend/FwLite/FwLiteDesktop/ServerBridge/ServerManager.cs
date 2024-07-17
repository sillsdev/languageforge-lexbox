using LocalWebApp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FwLiteDesktop.ServerBridge;

public class ServerManager(Action<WebApplicationBuilder>? configure = null) : IAsyncDisposable
{
    private readonly TaskCompletionSource<WebApplication> _started = new();
    public Task<WebApplication> Started => _started.Task;
    private WebApplication? _webApp;

    public void Start(ILogger<ServerManager> logger)
    {
        _webApp = LocalWebAppServer.SetupAppServer([], configure);
        _ = Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("Starting web app");
                await _webApp.StartAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to start web app");
                throw;
            }
            logger.LogInformation("Web app started");
            _started.SetResult(_webApp);
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_webApp is null) return;
        await _webApp.StopAsync(TimeSpan.FromSeconds(10));
        await _webApp.DisposeAsync();
    }
}
