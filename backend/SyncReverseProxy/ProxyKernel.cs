using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using LexCore.Auth;
using LexCore.Config;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexSyncReverseProxy.Auth;
using LexSyncReverseProxy.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Yarp.ReverseProxy.Forwarder;

namespace LexSyncReverseProxy;

public static class ProxyKernel
{
    public const string UserHasAccessToProjectPolicy = "UserHasAccessToProject";

    public static void AddSyncProxy(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ProxyEventsService>();
        services.AddMemoryCache();
        services.AddScoped<IAuthorizationHandler, UserHasAccessToProjectRequirementHandler>();
        services.AddTelemetryConsumer<ForwarderTelemetryConsumer>();
        services.AddSingleton(new HgRequestTransformer());
        services.AddForwarder();

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>(BasicAuthHandler.AuthScheme, null);
    }

    public static void AddForwarder(this IServiceCollection services)
    {
        services.AddHttpForwarder();
        services.TryAddSingleton(new HttpMessageInvoker(new SocketsHttpHandler
        {
            UseProxy = false,
            UseCookies = false,
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout = TimeSpan.FromSeconds(15)
        }));
    }

    public static void MapSyncProxy(this IEndpointRouteBuilder app,
        string? extraAuthScheme = null)
    {

        var authorizeAttribute = new AuthorizeAttribute
        {
            AuthenticationSchemes = string.Join(',', BasicAuthHandler.AuthScheme, extraAuthScheme ?? ""),
            Policy = UserHasAccessToProjectPolicy
        };
        //hgresumable
        app.Map("/api/v03/{**catch-all}",
            async (HttpContext context, [FromQuery(Name = "repoId")] string projectCode) =>
            {
                await Forward(context, projectCode);
            }).RequireAuthorization(authorizeAttribute).WithMetadata(HgType.resumable);

        //hgweb
        app.Map($"/{{{ProxyConstants.HgProjectCodeRouteKey}}}/{{**catch-all}}",
            async (HttpContext context, [FromRoute(Name = ProxyConstants.HgProjectCodeRouteKey)] string projectCode) =>
            {
                await Forward(context, projectCode);
            }).RequireAuthorization(authorizeAttribute).WithMetadata(HgType.hgWeb);
        app.Map($"/hg/{{{ProxyConstants.HgProjectCodeRouteKey}}}/{{**catch-all}}",
            async (HttpContext context, [FromRoute(Name = ProxyConstants.HgProjectCodeRouteKey)] string projectCode) =>
            {
                await Forward(context, projectCode);
            }).RequireAuthorization(authorizeAttribute).WithMetadata(HgType.hgWeb);
        app.Map($"/hg/{{first-letter:length(1)}}/{{{ProxyConstants.HgProjectCodeRouteKey}}}/{{**catch-all}}",
            async (HttpContext context, [FromRoute(Name = ProxyConstants.HgProjectCodeRouteKey)] string projectCode) =>
            {
                await Forward(context, projectCode);
            }).RequireAuthorization(authorizeAttribute).WithMetadata(HgType.hgWeb);
    }



    private static async Task Forward(HttpContext context,
        string projectCode)
    {
        Activity.Current?.AddTag("app.project_code", projectCode);
        Activity.Current?.AddTag("app.send_receive", true);
        var httpClient = context.RequestServices.GetRequiredService<HttpMessageInvoker>();
        var forwarder = context.RequestServices.GetRequiredService<IHttpForwarder>();
        var eventsService = context.RequestServices.GetRequiredService<ProxyEventsService>();
        var hgService = context.RequestServices.GetRequiredService<IHgService>();
        var transformer = context.RequestServices.GetRequiredService<HgRequestTransformer>();
        var lexProxyService = context.RequestServices.GetRequiredService<ILexProxyService>();
        var sendReceiveService = context.RequestServices.GetRequiredService<ISendReceiveService>();
        var hgType = context.GetEndpoint()?.Metadata.OfType<HgType>().FirstOrDefault() ?? throw new ArgumentException("Unknown HG request type");

        var requestInfo = lexProxyService.GetDestinationPrefix(hgType);

        // userId (the "sub" claim) is always present here: these routes require BasicAuth, so the
        // concurrency key is always well-formed and per-user.
        var userId = context.User.FindFirstValue(LexAuthConstants.IdClaimType) ?? string.Empty;
        var gate = await sendReceiveService.BeginSendReceive(projectCode, userId);
        using var sendReceiveTicket = gate.Ticket;
        switch (gate.Result)
        {
            case BeginSendReceiveResult.MigrationInProgress:
                throw new ProjectLockedException(projectCode);
            case BeginSendReceiveResult.ConcurrencyLimitReached:
                Activity.Current?.AddTag("app.send_receive_concurrency_limited", true);
                await WriteConcurrencyLimitReached(context);
                return;
        }

        await forwarder.SendAsync(context, requestInfo.DestinationPrefix, httpClient, ForwarderRequestConfig.Empty, transformer);
        try
        {
            switch (hgType)
            {
                case HgType.hgWeb:
                    await eventsService.OnHgRequest(context);
                    break;
                case HgType.resumable:
                    await eventsService.OnResumableRequest(context);
                    break;
            }

            if (hgService.HasAbandonedTransactions(projectCode))
                Activity.Current?.AddTag("app.abandoned_transaction_detected", true);
        }
        catch (Exception e)
        {
            Activity.Current?.AddException(e);
            //we don't want to throw errors from the post process event
        }
    }

    private static async Task WriteConcurrencyLimitReached(HttpContext context)
    {
        var config = context.RequestServices.GetRequiredService<IOptions<SendReceiveConfig>>().Value;
        var (statusCode, retryAfterSeconds) = ResolveConcurrencyLimitResponse(
            context.Request.Headers.UserAgent.ToString(),
            config.ConcurrencyRetryAfterSeconds);
        context.Response.StatusCode = statusCode;
        if (retryAfterSeconds is { } seconds)
            context.Response.Headers.RetryAfter = seconds.ToString();
        // Body must be non-empty: an old resumable client maps a 503-with-body to NotAvailable and
        // stops cleanly.
        await context.Response.WriteAsync(
            "Too many concurrent sync requests for this project; please try again shortly.");
    }

    /// <summary>
    /// Version-gated rejection status for the per-(user, project) concurrency cap. The Chorus
    /// resumable client infinite-retries on an unhandled 429, but stops cleanly on 503. Newer
    /// clients advertise a "Chorus/&lt;ver&gt;" capability token in their User-Agent and map 429 to
    /// backoff-and-retry, so only those receive 429 + Retry-After; everything else (old resumable
    /// clients sending "HgResume v03" with no token, and plain hgweb/mercurial clients) gets 503.
    /// </summary>
    public static (int statusCode, int? retryAfterSeconds) ResolveConcurrencyLimitResponse(string? userAgent, int retryAfterSeconds)
    {
        var clientHandles429 = userAgent?.Contains("Chorus/", StringComparison.OrdinalIgnoreCase) == true;
        return clientHandles429
            ? (StatusCodes.Status429TooManyRequests, retryAfterSeconds)
            : (StatusCodes.Status503ServiceUnavailable, null);
    }

    /// <summary>
    /// this is required because if resumable receives a 403 Forbidden,
    /// it will retry forever, we must return a 401 Unauthorized instead.
    /// Must be called after routing but before Auth so it can intercept and change the 403
    /// </summary>
    public static void UseResumableStatusHack(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            await next(context);
            if (context.Response.StatusCode != 403)
            {
                return;
            }

            var hgType = context.GetEndpoint()?.Metadata.OfType<HgType>().FirstOrDefault();
            if (hgType == HgType.resumable)
                context.Response.StatusCode = 401;
        });
    }
}
