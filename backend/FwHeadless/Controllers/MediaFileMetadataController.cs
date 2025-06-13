using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FwHeadless.Controllers;

public static class MediaFileMetadataController
{
    [HttpGet("/{fileId}")]
    public static async Task<Results<Ok<MediaFile>, NotFound>> GetFileMetadata(
        Guid fileId,
        LexBoxDbContext lexBoxDb)
    {
        var metadata = await lexBoxDb.Files.FindAsync(fileId);
        if (metadata is null) return TypedResults.NotFound();
        return TypedResults.Ok(metadata);
    }
}
