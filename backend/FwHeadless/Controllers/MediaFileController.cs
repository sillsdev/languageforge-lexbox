using FwHeadless.Models;
using FwHeadless.Services;
using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using Microsoft.Net.Http.Headers;

namespace FwHeadless.Controllers;

public static class MediaFileController
{
    public static async Task<Results<PhysicalFileHttpResult, NotFound>> GetFile(
        Guid fileId,
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
        mediaFile.InitializeMetadataIfNeeded(filePath);
        var contentType = mediaFile.Metadata.MimeType;
        if (contentType is null)
        {
            contentType = MimeMapping.MimeUtility.GetMimeMapping(filePath);
            mediaFile.Metadata.MimeType = contentType;
            await lexBoxDb.SaveChangesAsync();
        }
        if (await AddEntityTagMetadataIfNotPresent(mediaFile, filePath)) await lexBoxDb.SaveChangesAsync();
        var entityTag = EntityTagHeaderValue.Parse(mediaFile.Metadata.Sha256Hash!);
        return TypedResults.PhysicalFile(filePath, contentType, mediaFile.Filename, mediaFile.UpdatedDate, entityTag, enableRangeProcessing: true);
    }

    public static async Task<Results<Ok, NotFound>> DeleteFile(
        Guid fileId,
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
        File.Delete(filePath);
        lexBoxDb.Files.Remove(mediaFile);
        await lexBoxDb.SaveChangesAsync();
        return TypedResults.Ok();
    }

    public static async Task<Results<Ok<PostFileResult>, Created<PostFileResult>, NotFound, BadRequest>> PutFile(
        Guid fileId,
        [FromForm] Guid? projectId,
        [FromForm] string? filename,
        [FromForm] IFormFile file,
        [FromForm] FileMetadata? metadata,
        HttpContext httpContext,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb)
    {
        var result = await HandleFileUpload(fileId, projectId, filename, file, metadata, httpContext, config, lexBoxDb, newFilesAllowed: false, returnCreatedOnSuccess: false);
        return result;
    }

    }

    [HttpPost]
    public static async Task<Results<Ok<PostFileResult>, Created<PostFileResult>, NotFound, BadRequest>> PostFile(
        [FromForm] Guid? fileId,
        [FromForm] Guid projectId,
        [FromForm] string filename,
        [FromForm] IFormFile file,
        [FromForm] FileMetadata? metadata,
        HttpContext httpContext,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb)
    {
        var result = await HandleFileUpload(fileId, projectId, filename, file, metadata, httpContext, config, lexBoxDb, newFilesAllowed: true, returnCreatedOnSuccess: true);
        return result;
    }

    public static async Task<Results<Ok<PostFileResult>, Created<PostFileResult>, NotFound, BadRequest>> HandleFileUpload(
        Guid? fileId,
        Guid? projectId,
        string? filename,
        IFormFile file,
        FileMetadata? metadata,
        HttpContext httpContext,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb,
        bool newFilesAllowed,
        bool returnCreatedOnSuccess)
    {
        bool replacedExistingFile = false;
        // Sanity check: reject ridiculously large uploads before doing any work
        var maxUploadSize = config.Value.MaxUploadFileSizeBytes;
        if ((httpContext.Request.ContentLength is not null && httpContext.Request.ContentLength > maxUploadSize) || file.Length > maxUploadSize)
        {
            return TypedResults.BadRequest();
        }
        if (fileId is null) fileId = Guid.NewGuid();
        if (metadata is null) metadata = new FileMetadata() { Filename = filename, SizeInBytes = 0 };
        var mediaFile = await lexBoxDb.Files.FindAsync(fileId);
        if (mediaFile is null && !newFilesAllowed)
        {
            return TypedResults.NotFound(); // PUT requests must modify an existing file and return 404 if it doesn't exist
        }
        if (mediaFile is null)
        {
            if (projectId is null)
            {
                // TODO: Create an error object that will be returned, explaining why the request is bad (e.g., here it's because projectId is required when not replacing existing files)
                return TypedResults.BadRequest();
            }
            if (string.IsNullOrEmpty(filename))
            {
                // TODO: Error message should be "filename is required if uploading new file"
                return TypedResults.BadRequest();
            }
            mediaFile = new MediaFile()
            {
                Id = fileId.Value,
                Filename = filename,
                ProjectId = projectId.Value,
                Metadata = metadata,
            };
            lexBoxDb.Files.Add(mediaFile);
        }
        else
        {
            replacedExistingFile = true;
            projectId ??= mediaFile.ProjectId;
            if (projectId != mediaFile.ProjectId)
            {
                // TODO: Decide whether we should allow existing files to be moved into a different project from the one they're currently in, or if that should be an error
                // TODO: If error, then error object here should specify that files cannot be moved from one project to another
                return TypedResults.BadRequest();
            }
            filename ??= mediaFile.Filename;
            if (filename != mediaFile.Filename)
            {
                // TODO: Handle renaming files appropriately: move existing file to the new name now, then WriteFileToDisk will overwrite it with new contents
            }
            if (mediaFile.Metadata is null) mediaFile.Metadata = metadata;
            else mediaFile.Metadata.Merge(metadata);
        }
        var project = await lexBoxDb.Projects.FindAsync(projectId);
        if (project is null) return TypedResults.NotFound();
        var projectFolder = config.Value.GetProjectFolder(project.Code, projectId.Value);
        if (projectFolder is null)
        {
            // TODO: Add error message to communicate "project folder not found, might need to add it to FW Lite before this will work"
            return TypedResults.BadRequest();
        }
        var filePath = Path.Join(projectFolder, filename);
        mediaFile.Metadata.Filename = filename;
        mediaFile.Metadata.SizeInBytes = (int)file.Length;
        try
        {
            await WriteFileToDisk(filePath, file.OpenReadStream());
        }
        catch (ArgumentOutOfRangeException)
        {
            // TODO: Add an error message here to communicate "Too large"
            return TypedResults.BadRequest();
        }
        await AddEntityTagMetadata(mediaFile, filePath);
        mediaFile.UpdateUpdatedDate();
        await lexBoxDb.SaveChangesAsync();
        var responseBody = new PostFileResult(fileId.Value);
        // TODO: Consider adding ETag to the POST results so uploaders could, in theory, save it and use it later in a GET operation
        if (replacedExistingFile)
        {
            return TypedResults.Ok(responseBody);
        }
        else
        {
            // TODO: Put "/api/media" into a constant so if it changes in MediaFileRoutes it will change here as well
            var newLocation = $"/api/media/{fileId}";
            return TypedResults.Created(newLocation, responseBody);
        }
    }

    static async Task<long> WriteFileToDisk(string filePath, Stream contents)
    {
        if (contents is null) return 0;
        long startPosition = 0;
        try
        {
            startPosition = contents.Position;
        }
        catch { }
        // TODO: Write to temp file, then move file into place, overwriting existing file
        // That way files will be replaced atomically, and a failure halfway through the process won't result in the existing file being lost
        var writeStream = File.OpenWrite(filePath);
        await contents.CopyToAsync(writeStream);
        await writeStream.DisposeAsync();
        long endPosition = 0;
        try
        {
            endPosition = contents.Position;
        }
        catch { }
        var calcLength = endPosition - startPosition;
        if (calcLength == 0)
        {
            // Either the stream was empty, or its Position attribute wasn't reliable, so we need
            // to look at the file we just wrote to determine the size
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }
        else
        {
            return calcLength;
        }
    }

    private static async Task AddEntityTagMetadata(MediaFile mediaFile, string filePath)
    {
        mediaFile.InitializeMetadataIfNeeded(filePath);
        var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream);
        mediaFile.Metadata.Sha256Hash = Convert.ToHexStringLower(hash);
    }

    private static async Task<bool> AddEntityTagMetadataIfNotPresent(MediaFile mediaFile, string filePath)
    {
        if (mediaFile.Metadata?.Sha256Hash is null)
        {
            await AddEntityTagMetadata(mediaFile, filePath);
            return true;
        }
        return false;
    }
}
