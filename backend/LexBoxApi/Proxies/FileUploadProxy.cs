using System.Diagnostics;
using Yarp.ReverseProxy.Forwarder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using LexBoxApi.Auth.Attributes;
using Microsoft.Extensions.Options;
using LexCore.Config;

namespace LexBoxApi.Proxies;

public static class FileUploadProxy
{
    public const string RequireScopePolicy = RequireScopeAttribute.PolicyName;

    public static void AddFileUploadProxy(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddTelemetryConsumer<LexSyncReverseProxy.ForwarderTelemetryConsumer>();
        services.AddSingleton(new HttpMessageInvoker(new SocketsHttpHandler
        {
            UseProxy = false,
            UseCookies = false,
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout = TimeSpan.FromSeconds(15)
        }));

        services.AddHttpForwarder();
    }

    public static void MapFileUploadProxy(this IEndpointRouteBuilder app,
        string? extraAuthScheme = null)
    {

        var authorizeAttribute = new AuthorizeAttribute
        {
            AuthenticationSchemes = string.Join(',', JwtBearerDefaults.AuthenticationScheme, extraAuthScheme ?? ""),
            Policy = RequireScopePolicy
        };

        //media upload/download
        app.Map("/api/list-media/{**catch-all}",
            Forward).RequireAuthorization(authorizeAttribute);

        //media upload/download
        app.Map("/api/media/{**catch-all}",
            Forward).RequireAuthorization(authorizeAttribute);

        //metadata requests
        app.Map("/api/metadata/{**catch-all}",
            Forward).RequireAuthorization(authorizeAttribute);
    }

    private static async Task Forward(HttpContext context)
    {
        Activity.Current?.AddTag("app.file_upload", true);
        var httpClient = context.RequestServices.GetRequiredService<HttpMessageInvoker>();
        var forwarder = context.RequestServices.GetRequiredService<IHttpForwarder>();
        var mediaFileConfig = context.RequestServices.GetRequiredService<IOptions<MediaFileConfig>>();

        var destinationPrefix = mediaFileConfig.Value.FwHeadlessUrl;

        await forwarder.SendAsync(context, destinationPrefix, httpClient, ForwarderRequestConfig.Empty);
    }
}
