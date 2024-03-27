using System.Diagnostics;
using System.Text;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexSyncReverseProxy.Auth;
using LexSyncReverseProxy.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        services.AddSingleton(new HttpMessageInvoker(new SocketsHttpHandler
        {
            UseProxy = false,
            UseCookies = false,
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout = TimeSpan.FromSeconds(15)
        }));
        services.AddSingleton(new HgRequestTransformer());

        services.AddHttpForwarder();
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>(BasicAuthHandler.AuthScheme, null);
        services.AddAuthorizationBuilder()
            .AddPolicy("UserHasAccessToProject",
                policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser()
                        .AddRequirements(new UserHasAccessToProjectRequirement());
                });
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

        using var sendReceiveTicket = await sendReceiveService.BeginSendReceive(projectCode);
        if (sendReceiveTicket is null) throw new ProjectLockedException(projectCode);

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
            Activity.Current?.RecordException(e);
            //we don't want to throw errors from the post process event
        }
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
