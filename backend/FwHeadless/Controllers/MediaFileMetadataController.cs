using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FwHeadless.Controllers;

public static class MediaFileMetadataController
{
    [HttpGet("/{fileId}")]
    public static async Task<Results<Ok<ApiMetadataEndpointResult>, NotFound>> GetFileMetadata(
        Guid fileId,
        LexBoxDbContext lexBoxDb)
    {
        var mediaFile = await lexBoxDb.Files.FindAsync(fileId);
        if (mediaFile is null) return TypedResults.NotFound();
        var result = new ApiMetadataEndpointResult(mediaFile.Metadata)
        {
            Filename = Path.GetFileName(mediaFile.Filename)
        };
        return TypedResults.Ok(result);
    }
}
