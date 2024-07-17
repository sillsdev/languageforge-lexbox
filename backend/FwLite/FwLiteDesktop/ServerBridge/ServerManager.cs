using LocalWebApp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace FwLiteDesktop;

public class ServerManager : IAsyncDisposable
{
    private readonly TaskCompletionSource<WebApplication> _started = new();
    public Task<WebApplication> Started => _started.Task;
    private WebApplication? _webApp;

    public void Start()
    {
        _webApp = LocalWebAppServer.SetupAppServer([]);
        _ = Task.Run(async () =>
        {
            try
            {
                await _webApp.StartAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

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
