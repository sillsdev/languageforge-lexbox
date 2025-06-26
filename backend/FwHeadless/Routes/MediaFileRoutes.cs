using FwHeadless.Controllers;

namespace FwHeadless.Routes;

public static class MediaFileRoutes
{
    public static IEndpointConventionBuilder MapMediaFileRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/media").WithOpenApi();
        group.MapGet("/list/{projectId:guid}", MediaFileController.ListFiles);
        group.MapGet("/metadata/{fileId:guid}", MediaFileMetadataController.GetFileMetadata);
        group.MapGet("/{fileId:guid}", MediaFileController.GetFile);
        group.MapPut("/{fileId:guid}", MediaFileController.PutFile).DisableAntiforgery();
        group.MapDelete("/{fileId:guid}", MediaFileController.DeleteFile);
        group.MapPost("/", MediaFileController.PostFile).DisableAntiforgery();
        return group;
    }
}
