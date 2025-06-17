using FwHeadless.Controllers;

namespace FwHeadless.Routes;

public static class MediaFileRoutes
{
    public static IEndpointConventionBuilder MapMediaFileRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithOpenApi();
        group.MapGet("/list-media/{projectId}", MediaFileController.ListFiles); // TODO: During code review, discuss what URL to assign this API endpoint
        group.MapGet("/media/{fileId}", MediaFileController.GetFile);
        group.MapPut("/media/{fileId}", MediaFileController.PutFile);
        group.MapDelete("/media/{fileId}", MediaFileController.DeleteFile);
        group.MapPost("/media/", MediaFileController.PostFile).DisableAntiforgery();
        group.MapGet("/metadata/{fileId}", MediaFileMetadataController.GetFileMetadata);
        return group;
    }
}
