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

    public static IEndpointConventionBuilder MapFileUploadProxy(this IEndpointRouteBuilder app)
    {

        var authorizeForUploadAttribute = new AuthorizeAttribute
        {
            Policy = UserCanUploadMediaFilesPolicy
        };

        var authorizeForDownloadAttribute = new AuthorizeAttribute
        {
            Policy = UserCanDownloadMediaFilesPolicy
        };

        var group = app.MapGroup("/api/media").WithOpenApi();

        //media upload/download
        group.Map("/list/{projectId:guid}/{**catch-all}",
            Forward).RequireAuthorization(authorizeForDownloadAttribute);

        //media upload/download
        group.MapGet("/{fileId:guid}",
            Forward).RequireAuthorization(authorizeForDownloadAttribute);
        group.MapPut("/{fileId:guid}",
            Forward).RequireAuthorization(authorizeForUploadAttribute);
        group.MapPost("/",
            Forward).RequireAuthorization(authorizeForUploadAttribute);
        group.MapDelete("/{fileId:guid}",
            Forward).RequireAuthorization(authorizeForUploadAttribute);

        //metadata requests
        group.Map("/metadata/{fileId:guid}",
            Forward).RequireAuthorization(authorizeForDownloadAttribute);

        return group;
    }

    private static async Task Forward(HttpContext context)
    {
        Activity.Current?.AddTag("app.file_upload", true);
        var httpClient = context.RequestServices.GetRequiredService<HttpMessageInvoker>();
        var forwarder = context.RequestServices.GetRequiredService<IHttpForwarder>();
        var mediaFileConfig = context.RequestServices.GetRequiredService<IOptions<MediaFileConfig>>();

        var destinationPrefix = mediaFileConfig.Value.FwHeadlessUrl;

        var error = await forwarder.SendAsync(context, destinationPrefix, httpClient, ForwarderRequestConfig.Empty);
        if (context.Response.StatusCode == 502 && error.HasFlag(ForwarderError.RequestBodyDestination))
        {
            // FwHeadless will close the request early with a 413 if it determines the content-type is too large,
            // but IHttpForwarder rewrites the status code to 502 if that happens. This is not a Bad Gateway error,
            // so we really want a 413 in this specific case.
            context.Response.StatusCode = 413;
        }
    }
}
