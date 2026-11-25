using System.Diagnostics;
using System.Net;
using System.Text.Json;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIL.Harmony;
using SIL.Harmony.Core;
using WatsonWebserver.Core;
using WatsonWebserver.Lite;
using WatsonMethod = WatsonWebserver.Core.HttpMethod;

namespace FwLiteShared.LocalSync.Hosting;

/// <summary>
/// The thin HTTP layer of a local sync host (Watson.Lite: small, and no http.sys, so the same server runs on Windows,
/// Linux and Android). It maps the join routes onto the <see cref="HostSession"/> and the Harmony sync routes, which
/// mirror Lexbox's so joiners reuse <see cref="LcmCrdt.RemoteSync.CrdtHttpSyncService"/>, onto <see cref="LocalSyncHost"/>.
/// </summary>
internal sealed class LocalSyncHttpServer : IDisposable
{
    private static readonly JsonSerializerOptions ProtocolJson = new(JsonSerializerDefaults.Web);
    private readonly HostSession _session;
    private readonly CrdtProject _project;
    private readonly IServiceProvider _services;
    private readonly JsonSerializerOptions _harmonyJson;
    private readonly ILogger _logger;
    private readonly string _address;
    private readonly WebserverLite _server;

    public LocalSyncHttpServer(HostSession session, CrdtProject project, IServiceProvider services,
        JsonSerializerOptions harmonyJson, ILogger logger, IPAddress bindTo, int port)
    {
        _session = session;
        _project = project;
        _services = services;
        _harmonyJson = harmonyJson;
        _logger = logger;
        _address = bindTo.ToString();
        _server = new WebserverLite(new WebserverSettings(bindTo.ToString(), port), NotFound);

        var routes = _server.Routes.PreAuthentication;
        routes.Static.Add(WatsonMethod.GET, LocalSyncProtocol.HealthPath, async ctx => await ctx.Response.Send("ok"));
        routes.Static.Add(WatsonMethod.POST, LocalSyncProtocol.HelloPath, Guarded(Hello, LogLevel.Debug));
        routes.Static.Add(WatsonMethod.POST, LocalSyncProtocol.JoinPath, Guarded(Join));
        routes.Parameter.Add(WatsonMethod.GET, LocalSyncProtocol.JoinPath + "/{requestId}", Guarded(JoinStatus));
        routes.Static.Add(WatsonMethod.POST, LocalSyncProtocol.LeavePath, Guarded(Leave));
        routes.Static.Add(WatsonMethod.POST, LocalSyncProtocol.WithdrawPath, Guarded(Withdraw));
        routes.Static.Add(WatsonMethod.GET, LocalSyncProtocol.ProjectPath, Guarded(ctx => Authorized(ctx, async _ =>
            await SendJson(ctx, await HostedProjectReader.Read(_services, _project), ProtocolJson))));
        routes.Parameter.Add(WatsonMethod.GET, "/api/crdt/{id}/get", Guarded(ctx => Sync(ctx, async host =>
            await SendJson(ctx, await host.GetSyncState(ProjectIdFrom(ctx)), _harmonyJson))));
        routes.Parameter.Add(WatsonMethod.POST, "/api/crdt/{id}/changes", Guarded(ctx => Sync(ctx, async host =>
        {
            var otherHeads = Deserialize<SyncState>(ctx, _harmonyJson);
            await SendJson(ctx, await host.GetChanges(ProjectIdFrom(ctx), otherHeads), _harmonyJson);
        })));
        routes.Parameter.Add(WatsonMethod.POST, "/api/crdt/{id}/add", Guarded(ctx => Sync(ctx, async host =>
        {
            await host.AddRange(ProjectIdFrom(ctx), Deserialize<Commit[]>(ctx, _harmonyJson));
            await ctx.Response.Send();
        })));
    }

    public void Start() => _server.Start();

    private async Task Hello(HttpContextBase ctx)
    {
        var hello = Deserialize<Hello>(ctx, ProtocolJson);
        // the host's own app finds itself when it searches; it isn't a nearby device
        if (ctx.Request.Source.IpAddress != _address) _session.SayHello(ctx.Request.Source.IpAddress, hello);
        await ctx.Response.Send();
    }

    private async Task Join(HttpContextBase ctx)
    {
        var request = Deserialize<JoinRequest>(ctx, ProtocolJson);
        Guid requestId;
        try
        {
            requestId = _session.RequestJoin(request, await HostedProjectReader.Read(_services, _project), ctx.Request.Source.IpAddress);
        }
        catch (HostSessionFullException)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            await ctx.Response.Send();
            return;
        }
        await SendJson(ctx, new JoinResponse(requestId), ProtocolJson);
    }

    private async Task JoinStatus(HttpContextBase ctx)
    {
        var status = Guid.TryParse(ctx.Request.Url.Parameters["requestId"], out var requestId) ? _session.GetJoinStatus(requestId) : null;
        if (status is null)
        {
            await NotFound(ctx);
            return;
        }
        await SendJson(ctx, status, ProtocolJson);
    }

    private async Task Leave(HttpContextBase ctx)
    {
        if (Token(ctx) is { } token) _session.Leave(token);
        await ctx.Response.Send();
    }

    // the request id is only ever sent to the device that asked, so it's proof enough
    private async Task Withdraw(HttpContextBase ctx)
    {
        _session.Withdraw(Deserialize<JoinResponse>(ctx, ProtocolJson).RequestId);
        await ctx.Response.Send();
    }

    /// <summary>Runs a route only for a joiner the host approved in this session.</summary>
    private async Task Authorized(HttpContextBase ctx, Func<string, Task> handle)
    {
        var token = Token(ctx);
        if (token is null || !_session.IsAuthorized(token))
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await ctx.Response.Send();
            return;
        }
        await handle(token);
    }

    /// <summary>Runs a Harmony sync route for an approved joiner, in a scope for the hosted project.</summary>
    private async Task Sync(HttpContextBase ctx, Func<LocalSyncHost, Task> handle) => await Authorized(ctx, async token =>
    {
        await using var scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContext(_project);
        try
        {
            await handle(scope.ServiceProvider.GetRequiredService<LocalSyncHost>());
            _session.MarkSynced(token);
        }
        catch (LocalSyncProjectNotFoundException)
        {
            await NotFound(ctx);
        }
    });

    /// <param name="logLevel">Every request is logged, so a joiner's trouble can be traced from the host's log too.</param>
    private Func<HttpContextBase, Task> Guarded(Func<HttpContextBase, Task> handler, LogLevel logLevel = LogLevel.Information) => async ctx =>
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            await handler(ctx);
            _logger.Log(logLevel, "Local sync {Method} {Path} from {Source}: {Status} in {Elapsed} ms",
                ctx.Request.Method, ctx.Request.Url.RawWithoutQuery, ctx.Request.Source.IpAddress, ctx.Response.StatusCode,
                (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
        catch (JsonException e)
        {
            _logger.LogInformation(e, "Bad local sync request to {Url}", ctx.Request.Url.RawWithQuery);
            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await ctx.Response.Send();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Local sync request to {Url} failed", ctx.Request.Url.RawWithQuery);
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await ctx.Response.Send();
        }
    };

    private static Guid ProjectIdFrom(HttpContextBase ctx) =>
        Guid.TryParse(ctx.Request.Url.Parameters["id"], out var id) ? id : Guid.Empty;

    private static string? Token(HttpContextBase ctx)
    {
        var header = ctx.Request.RetrieveHeaderValue("Authorization");
        const string bearer = "Bearer ";
        return header is not null && header.StartsWith(bearer, StringComparison.OrdinalIgnoreCase) ? header[bearer.Length..] : null;
    }

    private static T Deserialize<T>(HttpContextBase ctx, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<T>(ctx.Request.DataAsString, options) ?? throw new JsonException("Empty request body");

    private static async Task SendJson<T>(HttpContextBase ctx, T value, JsonSerializerOptions options)
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.Send(JsonSerializer.Serialize(value, options));
    }

    private static async Task NotFound(HttpContextBase ctx)
    {
        ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
        await ctx.Response.Send();
    }

    public void Dispose()
    {
        if (_server.IsListening) _server.Stop();
        _server.Dispose();
    }
}
