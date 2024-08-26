using LcmCrdt;
using LocalWebApp.Auth;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using MiniLcm.Push;

namespace LocalWebApp.Services;

public class LexboxProjectService(
    AuthHelpersFactory helpersFactory,
    ILogger<LexboxProjectService> logger,
    IHttpMessageHandlerFactory httpMessageHandlerFactory,
    BackgroundSyncService backgroundSyncService,
    IMemoryCache cache)
{
    public record LexboxCrdtProject(Guid Id, string Name);

    public async Task<LexboxCrdtProject[]> GetLexboxProjects()
    {
        var httpClient = await helpersFactory.GetDefault().CreateClient();
        if (httpClient is null) return [];
        try
        {
            return await httpClient.GetFromJsonAsync<LexboxCrdtProject[]>("api/crdt/listProjects") ?? [];
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Error getting lexbox projects");
            return [];
        }
    }

    public async Task<Guid?> GetLexboxProjectId(string code)
    {
        var httpClient = await helpersFactory.GetDefault().CreateClient();
        if (httpClient is null) return null;
        try
        {
            return (await httpClient.GetFromJsonAsync<Guid?>($"api/crdt/lookupProjectId?code={code}"));
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Error getting lexbox project id");
            return null;
        }
    }

    public async Task ListenForProjectChanges(ProjectData projectData, CancellationToken stoppingToken)
    {
        if (string.IsNullOrEmpty(projectData.OriginDomain)) return;
        var lexboxConnection = await StartLexboxProjectChangeListener(projectData.OriginDomain, stoppingToken);
        if (lexboxConnection is null) return;
        await lexboxConnection.SendAsync("ListenForProjectChanges", projectData.Id, stoppingToken);
    }

    private string CacheKey(string originDomain) => $"LexboxProjectChangeListener|{originDomain}";

    public async Task<HubConnection?> StartLexboxProjectChangeListener(string originDomain,
        CancellationToken stoppingToken)
    {
        HubConnection? connection;
        if (cache.TryGetValue(CacheKey(originDomain), out connection) && connection is not null)
        {
            return connection;
        }

        if (await helpersFactory.GetHelper(new Uri(originDomain)).GetCurrentToken() is null)
        {

            logger.LogWarning("Unable to create signalR client, user is not authenticated to {OriginDomain}", originDomain);
            return null;
        }

        connection = new HubConnectionBuilder()
            //todo bridge logging to the aspnet logger
            .ConfigureLogging(logging => logging.AddConsole())
            .WithAutomaticReconnect()
            .WithUrl($"{originDomain}/api/hub/crdt/project-changes",
                connectionOptions =>
                {
                    connectionOptions.HttpMessageHandlerFactory = handler =>
                    {
                        //use a client that does not validate certs in dev
                        return httpMessageHandlerFactory.CreateHandler(AuthHelpers.AuthHttpClientName);
                    };
                    connectionOptions.AccessTokenProvider =
                        async () => await helpersFactory.GetHelper(new Uri(originDomain)).GetCurrentToken();
                })
            .Build();

        //it would be cleaner to pass the callback in to this method however it's not supposed to be generic, it should always trigger a sync
        connection.On(nameof(IProjectChangeListener.OnProjectUpdated),
            (Guid projectId) =>
            {
                logger.LogInformation("Received project update for {ProjectId}, triggering sync", projectId);
                backgroundSyncService.TriggerSync(projectId);
                return Task.CompletedTask;
            });

        try
        {
            //todo handle failure better, retry, maybe when a project sync is triggered.
            await connection.StartAsync(stoppingToken);

            connection.Closed += async (exception) =>
            {
                cache.Remove(CacheKey(originDomain));
                await connection.DisposeAsync();
            };
            cache.CreateEntry(CacheKey(originDomain)).SetValue(connection).RegisterPostEvictionCallback(
                static (key, value, reason, state) =>
                {
                    if (value is HubConnection con)
                    {
                        _ = con.DisposeAsync();
                    }
                });
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to start Lexbox listener");
            return null;
        }

        return connection;
    }
}
