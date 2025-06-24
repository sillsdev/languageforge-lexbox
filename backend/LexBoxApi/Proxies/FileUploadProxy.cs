using System.Diagnostics;
using Yarp.ReverseProxy.Forwarder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.Auth.Requirements;
using Microsoft.Extensions.Options;
using LexCore.Config;
using LexSyncReverseProxy;

namespace LexBoxApi.Proxies;

public static class FileUploadProxy
{
    public const string UserCanUploadMediaFilesPolicy = "UserCanUploadMediaFiles";
    public const string UserCanDownloadMediaFilesPolicy = "UserCanDownloadMediaFiles";

    public static void AddFileUploadProxy(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, MediaFileRequirementHandler>();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddForwarder();
    }

    public static void MapFileUploadProxy(this IEndpointRouteBuilder app)
    {

        var authorizeForUploadAttribute = new AuthorizeAttribute
        {
            Policy = UserCanUploadMediaFilesPolicy
        };

        var authorizeForDownloadAttribute = new AuthorizeAttribute
        {
            Policy = UserCanDownloadMediaFilesPolicy
        };

        //media upload/download
        app.Map("/api/list-media/{projectId:guid}/{**catch-all}",
            Forward).RequireAuthorization(authorizeForDownloadAttribute);

        //media upload/download
        app.MapGet("/api/media/{fileId:guid}",
            Forward).RequireAuthorization(authorizeForDownloadAttribute);
        app.MapPut("/api/media/{fileId:guid}",
            Forward).RequireAuthorization(authorizeForUploadAttribute);
        app.MapPost("/api/media/",
            Forward).RequireAuthorization(authorizeForUploadAttribute);
        app.MapDelete("/api/media/{fileId:guid}",
            Forward).RequireAuthorization(authorizeForUploadAttribute);

        //metadata requests
        app.Map("/api/metadata/{fileId:guid}",
            Forward).RequireAuthorization(authorizeForDownloadAttribute);
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
