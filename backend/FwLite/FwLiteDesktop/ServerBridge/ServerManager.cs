using FwDataMiniLcmBridge;
using LocalWebApp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FwLiteDesktop.ServerBridge;

public class ServerManager(Action<WebApplicationBuilder>? configure = null) : IMauiInitializeService
{
    private readonly TaskCompletionSource<WebApplication> _started = new();
    public Task<WebApplication> Started => _started.Task;
    private WebApplication? _webApp;
    private ILogger<ServerManager>? _logger;
    private Thread? _thread;
    private readonly ManualResetEvent _stop = new(false);
    public IServiceProvider WebServices => _webApp?.Services ?? throw new ApplicationException("initlize not yet called");

    public void Initialize(IServiceProvider services)
    {
        Start(services.GetRequiredService<ILogger<ServerManager>>());
    }

    private void Start(ILogger<ServerManager> logger)
    {
        _logger = logger;
        _webApp = LocalWebAppServer.SetupAppServer([], configure);
        _thread = new Thread(() =>
        {
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
                    _webApp.DisposeAsync().GetAwaiter().GetResult();
                    throw;
                }

                logger.LogInformation("Web app started");
                _started.SetResult(_webApp);
            });
            _stop.WaitOne();
            _webApp.StopAsync().GetAwaiter().GetResult();
            _webApp.DisposeAsync().GetAwaiter().GetResult();
            _webApp = null;
            logger.LogInformation("Web app fully shutdown");
        });
        _thread.IsBackground = false;
        _thread.Start();
    }

    public void Stop()
    {
        _logger?.LogInformation("Stopping ServerManager");
        _stop.Set();
    }

    ~ServerManager()
    {
        _stop.Set();
    }
}
