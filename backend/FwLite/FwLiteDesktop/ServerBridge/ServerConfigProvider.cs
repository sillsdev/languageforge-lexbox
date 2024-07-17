using Microsoft.Extensions.Configuration;

namespace FwLiteDesktop.ServerBridge;

public class ServerConfigSource: IConfigurationSource
{
    public ServerManager? ServerManager { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (ServerManager is null)
            throw new InvalidOperationException("ServerManager is not set");
        return new ServerConfigProvider(ServerManager);
    }
}

public class ServerConfigProvider : ConfigurationProvider
{
    public ServerConfigProvider(ServerManager serverManager)
    {
        _ = serverManager.Started.ContinueWith(t =>
        {
            Data = new Dictionary<string, string?> { ["LocalWebApp:Url"] = t.Result.Urls.First() };
            OnReload();
        },
        scheduler: TaskScheduler.Default);
    }

    public override void Load()
    {
    }
}
