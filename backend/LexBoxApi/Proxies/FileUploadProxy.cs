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
    public const string RequireScopePolicy = RequireScopeAttribute.PolicyName;

    public static void AddFileUploadProxy(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, MediaFileRequirementHandler>();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddForwarder();
    }

    public static void MapFileUploadProxy(this IEndpointRouteBuilder app,
        string? extraAuthScheme = null)
    {

        var authorizeForUploadAttribute = new AuthorizeAttribute
        {
            AuthenticationSchemes = string.Join(',', CookieAuthenticationDefaults.AuthenticationScheme, extraAuthScheme ?? ""),
            Policy = UserCanUploadMediaFilesPolicy
        };

        var authorizeForDownloadAttribute = new AuthorizeAttribute
        {
            AuthenticationSchemes = string.Join(',', CookieAuthenticationDefaults.AuthenticationScheme, extraAuthScheme ?? ""),
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
            Forward).RequireAuthorization(authorizeForUploadAttribute); // TODO: Figure out how to extract projectId from form for the UserCanUploadMediaFiles handler to use
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
