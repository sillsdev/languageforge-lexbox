using FwHeadless.Controllers;

namespace FwHeadless.Routes;

public static class MediaFileRoutes
{
    public static IEndpointConventionBuilder MapMediaFileRoutes(this WebApplication app)
    {
        app.MapGet("/api/list-media/{projectId}", MediaFileController.ListFiles); // TODO: During code review, discuss what URL to assign this API endpoint
        app.MapGet("/api/metadata/{fileId}", MediaFileMetadataController.GetFileMetadata);
        var group = app.MapGroup("/api/media").WithOpenApi();
        group.MapGet("/{fileId}", MediaFileController.GetFile);
        group.MapPut("/{fileId}", MediaFileController.PutFile).DisableAntiforgery();
        group.MapDelete("/{fileId}", MediaFileController.DeleteFile);
        group.MapPost("/", MediaFileController.PostFile).DisableAntiforgery();
        return group;
    }
}
