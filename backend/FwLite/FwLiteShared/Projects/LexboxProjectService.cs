using System.Net.Http.Json;
using FwLiteShared.Auth;
using FwLiteShared.Events;
using FwLiteShared.Sync;
using LcmCrdt;
using LexCore.Entities;
using LexCore.Sync;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm.Push;
using SIL.Harmony.Core;
using System.Text.Json.Serialization;

namespace FwLiteShared.Projects;

// Can't use ProblemDetails from Microsoft.AspNetCore.Mvc because if you reference Microsoft.AspNetCore.App.Ref,
// then the FwLiteShared project (and four other projects) refuse to build with the error
// "The package Microsoft.AspNetCore.App.Ref 9.0.0 has a package type DotnetPlatform that is incompatible with this project."
// So we'll just copy the ProblemDetails definition over here
public class ProblemDetails
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("type")]
    [JsonPropertyOrder(-5)]
    public string? Type { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("title")]
    [JsonPropertyOrder(-4)]
    public string? Title { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("status")]
    [JsonPropertyOrder(-3)]
    public int? Status { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("detail")]
    [JsonPropertyOrder(-2)]
    public string? Detail { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("instance")]
    [JsonPropertyOrder(-1)]
    public string? Instance { get; set; }
}

public class LexboxProjectService : IDisposable
{
    private readonly OAuthClientFactory clientFactory;
    private readonly ILogger<LexboxProjectService> logger;
    private readonly IHttpMessageHandlerFactory httpMessageHandlerFactory;
    private readonly BackgroundSyncService backgroundSyncService;
    private readonly IOptions<AuthConfig> options;
    private readonly IMemoryCache cache;
    private readonly GlobalEventBus globalEventBus;
    private readonly IDisposable onAuthChangedSubscription;

    public LexboxProjectService(
        OAuthClientFactory clientFactory,
        ILogger<LexboxProjectService> logger,
        IHttpMessageHandlerFactory httpMessageHandlerFactory,
        BackgroundSyncService backgroundSyncService,
        IOptions<AuthConfig> options,
        IMemoryCache cache,
        GlobalEventBus globalEventBus)
    {
        this.clientFactory = clientFactory;
        this.logger = logger;
        this.httpMessageHandlerFactory = httpMessageHandlerFactory;
        this.backgroundSyncService = backgroundSyncService;
        this.options = options;
        this.cache = cache;
        this.globalEventBus = globalEventBus;
        onAuthChangedSubscription = globalEventBus.OnAuthenticationChanged.Subscribe((@event) =>
        {
            InvalidateProjectsCache(@event.Server);
        });
    }

    public void Dispose()
    {
        onAuthChangedSubscription.Dispose();
    }

    public LexboxServer[] Servers()
    {
        return options.Value.LexboxServers;
    }

    public LexboxServer? GetServer(ProjectData? projectData)
    {
        if (projectData is null) return null;
        return Servers().FirstOrDefault(s => s.Id == projectData.ServerId);
    }

    public async Task<FieldWorksLiteProject[]> GetLexboxProjects(LexboxServer server)
    {
        return await cache.GetOrCreateAsync(CacheKey(server),
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                var httpClient = await clientFactory.GetClient(server).CreateHttpClient();
                if (httpClient is null) return [];
                try
                {
                    return await httpClient.GetFromJsonAsync<FieldWorksLiteProject[]>("api/crdt/listProjects") ?? [];
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error getting lexbox projects");
                    return [];
                }
            }) ?? [];
    }

    public async Task<LexboxUser?> GetLexboxUser(LexboxServer server)
    {
        return await clientFactory.GetClient(server).GetCurrentUser();
    }

    private static string CacheKey(LexboxServer server)
    {
        return $"Projects|{server.Authority.Authority}";
    }

    public async Task<Guid?> GetLexboxProjectId(LexboxServer server, string code)
    {
        var httpClient = await clientFactory.GetClient(server).CreateHttpClient();
        if (httpClient is null) return null;
        try
        {
            return await httpClient.GetFromJsonAsync<Guid?>($"api/crdt/lookupProjectId?code={code}");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error getting lexbox project id");
            return null;
        }
    }

    public async Task<ProjectSyncStatus> GetLexboxSyncStatus(LexboxServer server, Guid projectId)
    {
        var httpClient = await clientFactory.GetClient(server).CreateHttpClient();
        if (httpClient is null) return ProjectSyncStatus.Unknown(ProjectSyncStatusErrorCode.NotLoggedIn);
        try
        {
            var status = await httpClient.GetFromJsonAsync<ProjectSyncStatus>($"api/fw-lite/sync/status/{projectId}");
            return status ?? ProjectSyncStatus.Unknown(ProjectSyncStatusErrorCode.Unknown, "No status returned from server");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error getting lexbox sync status");
            return ProjectSyncStatus.Unknown(ProjectSyncStatusErrorCode.Unknown, e.Message);
        }
    }

    public async Task<HttpResponseMessage?> TriggerLexboxSync(LexboxServer server, Guid projectId)
    {
        var httpClient = await clientFactory.GetClient(server).CreateHttpClient();
        if (httpClient is null) return null;
        try
        {
            return await httpClient.PostAsync($"api/fw-lite/sync/trigger/{projectId}", null);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error triggering lexbox sync");
            return null;
        }
    }

    public async Task<SyncResult?> AwaitLexboxSyncFinished(LexboxServer server, Guid projectId, int timeoutSeconds = 15 * 60)
    {
        var httpClient = await clientFactory.GetClient(server).CreateHttpClient();
        if (httpClient is null) return null;
        var giveUpAt = DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSeconds);
        while (giveUpAt > DateTime.UtcNow)
        {
            try
            {
                // Avoid 30-second timeout by retrying every 25 seconds until max time reached
                var result = await httpClient.GetAsync(
                        $"api/fw-lite/sync/await-sync-finished/{projectId}",
                        new CancellationTokenSource(TimeSpan.FromSeconds(25)).Token);
                if (result.IsSuccessStatusCode)
                {
                    return await result.Content.ReadFromJsonAsync<SyncResult?>();
                }
                else
                {
                    var problem = await result.Content.ReadFromJsonAsync<ProblemDetails>();
                    return new SyncResult(0, 0, problem?.Detail);
                }
            }
            catch (OperationCanceledException) { continue; }
            catch (Exception e)
            {
                logger.LogError(e, "Error waiting for lexbox sync to finish");
                return null;
            }
        }
        logger.LogError("Timed out waiting for lexbox sync to finish");
        return null;
    }

    public async Task<int?> CountPendingCrdtCommits(LexboxServer server, Guid projectId, SyncState localSyncState)
    {
        var httpClient = await clientFactory.GetClient(server).CreateHttpClient();
        if (httpClient is null) return null;
        try
        {
            var result = await httpClient.PostAsJsonAsync<SyncState>($"/api/crdt/{projectId}/countChanges", localSyncState);
            var text = await result.Content.ReadAsStringAsync();
            return int.Parse(text);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error counting pending changes in lexbox");
            return null;
        }
    }

    public void InvalidateProjectsCache(LexboxServer server)
    {
        cache.Remove(CacheKey(server));
    }

    public async Task ListenForProjectChanges(ProjectData projectData, CancellationToken stoppingToken)
    {
        if (!options.Value.TryGetServer(projectData, out var server)) return;
        var lexboxConnection = await StartLexboxProjectChangeListener(server, stoppingToken);
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

        if (await clientFactory.GetClient(server).GetCurrentToken() is null)
        {

            logger.LogWarning("Unable to create signalR client, user is not authenticated to {OriginDomain}", server.Authority);
            return null;
        }

        connection = new HubConnectionBuilder()
            //todo bridge logging to the aspnet logger
            .ConfigureLogging(logging => logging.AddConsole())
            .WithAutomaticReconnect()
            .WithUrl($"{server.Authority}api/hub/crdt/project-changes",
                connectionOptions =>
                {
                    connectionOptions.HttpMessageHandlerFactory = handler =>
                    {
                        //use a client that does not validate certs in dev
                        return httpMessageHandlerFactory.CreateHandler(OAuthClient.AuthHttpClientName);
                    };
                    connectionOptions.AccessTokenProvider =
                        async () => await clientFactory.GetClient(server).GetCurrentToken();
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
