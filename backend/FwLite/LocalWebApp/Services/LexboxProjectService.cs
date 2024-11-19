﻿using LcmCrdt;
using LocalWebApp.Auth;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using MiniLcm.Push;

namespace LocalWebApp.Services;

public class LexboxProjectService(
    AuthHelpersFactory helpersFactory,
    ILogger<LexboxProjectService> logger,
    IHttpMessageHandlerFactory httpMessageHandlerFactory,
    BackgroundSyncService backgroundSyncService,
    IOptions<AuthConfig> options,
    IMemoryCache cache)
{
    public record LexboxProject(Guid Id, string Code, string Name, bool IsFwDataProject, bool IsCrdtProject);

    public LexboxServer[] Servers()
    {
        return options.Value.LexboxServers;
    }

    public async Task<LexboxProject[]> GetLexboxProjects(LexboxServer server)
    {
        return await cache.GetOrCreateAsync(CacheKey(server),
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                var httpClient = await helpersFactory.GetHelper(server).CreateClient();
                if (httpClient is null) return [];
                try
                {
                    return await httpClient.GetFromJsonAsync<LexboxProject[]>("api/crdt/listProjects") ?? [];
                }
                catch (HttpRequestException e)
                {
                    logger.LogError(e, "Error getting lexbox projects");
                    return [];
                }
            }) ?? [];
    }

    private static string CacheKey(LexboxServer server)
    {
        return $"Projects|{server.Authority.Authority}";
    }

    public async Task<Guid?> GetLexboxProjectId(LexboxServer server, string code)
    {
        var httpClient = await helpersFactory.GetHelper(server).CreateClient();
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

    public void InvalidateProjectsCache(LexboxServer server)
    {
        cache.Remove(CacheKey(server));
    }

    public async Task ListenForProjectChanges(ProjectData projectData, CancellationToken stoppingToken)
    {
        if (string.IsNullOrEmpty(projectData.OriginDomain)) return;
        var lexboxConnection = await StartLexboxProjectChangeListener(options.Value.GetServer(projectData), stoppingToken);
        if (lexboxConnection is null) return;
        await lexboxConnection.SendAsync("ListenForProjectChanges", projectData.Id, stoppingToken);
    }

    private static string HubConnectionCacheKey(LexboxServer server) => $"LexboxProjectChangeListener|{server.Authority.Authority}";

    public async Task<HubConnection?> StartLexboxProjectChangeListener(LexboxServer server,
        CancellationToken stoppingToken)
    {
        HubConnection? connection;
        if (cache.TryGetValue(HubConnectionCacheKey(server), out connection) && connection is not null)
        {
            return connection;
        }

        if (await helpersFactory.GetHelper(server).GetCurrentToken() is null)
        {

            logger.LogWarning("Unable to create signalR client, user is not authenticated to {OriginDomain}", server.Authority);
            return null;
        }

        connection = new HubConnectionBuilder()
            //todo bridge logging to the aspnet logger
            .ConfigureLogging(logging => logging.AddConsole())
            .WithAutomaticReconnect()
            .WithUrl($"{server.Authority}/api/hub/crdt/project-changes",
                connectionOptions =>
                {
                    connectionOptions.HttpMessageHandlerFactory = handler =>
                    {
                        //use a client that does not validate certs in dev
                        return httpMessageHandlerFactory.CreateHandler(AuthHelpers.AuthHttpClientName);
                    };
                    connectionOptions.AccessTokenProvider =
                        async () => await helpersFactory.GetHelper(server).GetCurrentToken();
                })
            .Build();

        //it would be cleaner to pass the callback in to this method however it's not supposed to be generic, it should always trigger a sync
        connection.On(nameof(IProjectChangeListener.OnProjectUpdated),
            (Guid projectId, Guid? clientId) =>
            {
                logger.LogInformation("Received project update for {ProjectId}, triggering sync", projectId);
                backgroundSyncService.TriggerSync(projectId, clientId);
                return Task.CompletedTask;
            });

        try
        {
            //todo handle failure better, retry, maybe when a project sync is triggered.
            await connection.StartAsync(stoppingToken);

            connection.Closed += async (exception) =>
            {
                cache.Remove(HubConnectionCacheKey(server));
                await connection.DisposeAsync();
            };
            cache.CreateEntry(HubConnectionCacheKey(server)).SetValue(connection).RegisterPostEvictionCallback(
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
