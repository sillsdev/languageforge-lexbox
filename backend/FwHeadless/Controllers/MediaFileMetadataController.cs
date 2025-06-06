using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FwHeadless.Controllers;

[ApiController]
[Route("/api/metadata")]
public class MediaFileMetadataController : ControllerBase
{
    [HttpGet("/{fileId}")]
    async Task<Results<Ok<MediaFile>, NotFound>> GetFileMetadata(
        Guid fileId,
        LexBoxDbContext lexBoxDb)
    {
        var metadata = await lexBoxDb.Files.FindAsync(fileId);
        if (metadata is null) return TypedResults.NotFound();
        return TypedResults.Ok(metadata);
    }
}
