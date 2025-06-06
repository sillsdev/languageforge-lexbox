using FwHeadless.Models;
using FwHeadless.Services;
using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace FwHeadless.Controllers;

[ApiController]
[Route("/api/media")]
// TODO: RequireScope? Which scope should we require here? Decide on auth story
public class MediaFileController : ControllerBase
{
    [HttpGet("/{fileId}")]
    async Task<Results<Ok<FileStream>, NotFound>> GetFile(
        Guid fileId,
        ProjectLookupService projectLookupService,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb)
    {
        var mediaFile = await lexBoxDb.Files.FindAsync(fileId);
        if (mediaFile is null) return TypedResults.NotFound();
        var projectId = mediaFile.ProjectId;
        var project = await lexBoxDb.Projects.FindAsync(projectId);
        if (project is null) return TypedResults.NotFound();
        var projectFolder = config.Value.GetProjectFolder(project.Code, projectId);
        var filePath = Path.Join(projectFolder, mediaFile.Filename);
        var file = System.IO.File.OpenRead(filePath);
        return TypedResults.Ok(file);
    }

    [HttpPut("/{fileId}")]
    async Task<Results<Ok, NotFound>> PutFile(
        Guid fileId,
        Stream body,
        ProjectLookupService projectLookupService,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb)
    {
        var mediaFile = await lexBoxDb.Files.FindAsync(fileId);
        if (mediaFile is null) return TypedResults.NotFound();
        var projectId = mediaFile.ProjectId;
        var project = await lexBoxDb.Projects.FindAsync(projectId);
        if (project is null) return TypedResults.NotFound();
        var projectFolder = config.Value.GetProjectFolder(project.Code, projectId);
        var filePath = Path.Join(projectFolder, mediaFile.Filename);
        var writeStream = System.IO.File.OpenWrite(filePath);
        await body.CopyToAsync(writeStream);
        mediaFile.UpdateUpdatedDate();
        lexBoxDb.SaveChanges();
        return TypedResults.Ok();
        // TODO: Extract out common code between this and Post, e.g. for file-size handling, because right now POST has some features that PUT does not
    }

    [HttpPost]
    async Task<Results<Created<PostFileResult>, NotFound, BadRequest>> PostFile(
        [FromForm] Guid fileId,
        [FromForm] Guid projectId,
        [FromForm] string filename,
        [FromForm] Stream file,
        [FromForm] FileMetadata metadata,
        HttpRequest request,
        ProjectLookupService projectLookupService,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb)
    {
        // Sanity check: reject ridiculously large uploads before doing any work
        if (request.ContentLength is not null && request.ContentLength > Int32.MaxValue)
        {
            // TODO: Decide on a sane limit, e.g. we won't accept uploads of more than 1 GB, and use that instead of Int32.MaxValue
            return TypedResults.BadRequest();
        }
        var mediaFile = await lexBoxDb.Files.FindAsync(fileId);
        if (mediaFile is null)
        {
            mediaFile = new MediaFile()
            {
                FileId = fileId,
                Filename = filename,
                ProjectId = projectId,
                Metadata = metadata,
            };
        }
        else
        {
            if (mediaFile.Metadata is null) mediaFile.Metadata = metadata;
            else mediaFile.Metadata.Merge(metadata);
            // TODO: Catch attempts to change projectId or filename and handle appropriately (moving file to different project? error?
            // TODO: Changing filename should be detected and should cause a rename, or a deletion of the old file
        }
        var project = await lexBoxDb.Projects.FindAsync(projectId);
        if (project is null) return TypedResults.NotFound();
        var projectFolder = config.Value.GetProjectFolder(project.Code, projectId);
        var filePath = Path.Join(projectFolder, mediaFile.Filename);
        var writeStream = System.IO.File.OpenWrite(filePath);
        await file.CopyToAsync(writeStream);
        writeStream.Dispose();
        // Get size of what we just wrote (don't want to rely on Content-Length header because it includes other form fields, not just the file contents)
        var fileInfo = new FileInfo(filePath);
        // TODO: Decide on max upload size and reject it
        if (fileInfo.Length > Int32.MaxValue)
        {
            fileInfo.Delete(); // Don't allow denial of service by uploading ridiculously large files
            return TypedResults.BadRequest();
        }
        else
        {
            mediaFile.Metadata.SizeInBytes = (int)fileInfo.Length;
        }
        mediaFile.UpdateUpdatedDate();
        lexBoxDb.SaveChanges();
        // TODO: Construct URL with appropriate ASP.NET Core methods rather than hardcoded string
        var newLocation = $"/api/media/{fileId}";
        var responseBody = new PostFileResult(fileId);
        return TypedResults.Created(newLocation, responseBody);
    }

}
