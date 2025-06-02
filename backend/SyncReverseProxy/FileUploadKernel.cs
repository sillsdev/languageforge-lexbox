using System.Diagnostics;
using Yarp.ReverseProxy.Forwarder;

namespace LexSyncReverseProxy;

public static class FileUploadKernel
{
    public const string UserHasAccessToProjectPolicy = ProxyKernel.UserHasAccessToProjectPolicy;

    public static void AddFileUploadProxy(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddTelemetryConsumer<ForwarderTelemetryConsumer>();
        services.AddSingleton(new HttpMessageInvoker(new SocketsHttpHandler
        {
            UseProxy = false,
            UseCookies = false,
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout = TimeSpan.FromSeconds(15)
        }));

        services.AddHttpForwarder();
        // services.AddAuthentication()
        //     .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>(BasicAuthHandler.AuthScheme, null);
    }

    public static void MapFileUploadProxy(this IEndpointRouteBuilder app,
        string? extraAuthScheme = null)
    {

        // No authorization... for now
        // var authorizeAttribute = new AuthorizeAttribute
        // {
        //     AuthenticationSchemes = string.Join(',', BasicAuthHandler.AuthScheme, extraAuthScheme ?? ""),
        //     Policy = UserHasAccessToProjectPolicy
        // };

        //media upload/download
        app.Map("/api/media/{**catch-all}",
            async (HttpContext context) =>
            {
                await Forward(context);
            }); //.RequireAuthorization(authorizeAttribute).WithMetadata(HgType.resumable);

        //metadata requests
        app.Map("/api/metadata/{**catch-all}",
            async (HttpContext context) =>
            {
                await Forward(context);
            }); //.RequireAuthorization(authorizeAttribute).WithMetadata(HgType.resumable);
    }

    private static async Task Forward(HttpContext context)
    {
        Activity.Current?.AddTag("app.file_upload", true);
        var httpClient = context.RequestServices.GetRequiredService<HttpMessageInvoker>();
        var forwarder = context.RequestServices.GetRequiredService<IHttpForwarder>();
        var transformer = context.RequestServices.GetRequiredService<HgRequestTransformer>();

        var destinationPrefix = "fw-headless"; // TODO: Get from config

        await forwarder.SendAsync(context, destinationPrefix, httpClient, ForwarderRequestConfig.Empty, transformer);
    }
}
