using FwHeadless.Models;
using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using Microsoft.Net.Http.Headers;
using System.Globalization;

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

    public static async Task<Results<Ok<PostFileResult>, Created<PostFileResult>, NotFound, BadRequest<FileUploadErrorMessage>, ProblemHttpResult>> PutFile(
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
    public static async Task<Results<Ok<PostFileResult>, Created<PostFileResult>, NotFound, BadRequest<FileUploadErrorMessage>, ProblemHttpResult>> PostFile(
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

    public static async Task<Results<Ok<PostFileResult>, Created<PostFileResult>, NotFound, BadRequest<FileUploadErrorMessage>, ProblemHttpResult>> HandleFileUpload(
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
            var detail = $"Max upload size is {maxUploadSize.ToString(NumberFormatInfo.InvariantInfo)} bytes, try reducing image quality or downsampling audio";
            return TypedResults.Problem(statusCode: 413, detail: detail);
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
                return TypedResults.BadRequest(FileUploadErrorMessage.ProjectIdRequiredForNewFiles);
            }
            if (string.IsNullOrEmpty(filename))
            {
                return TypedResults.BadRequest(FileUploadErrorMessage.FilenameRequiredForNewFiles);
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
                return TypedResults.BadRequest(FileUploadErrorMessage.UploadedFilesCannotBeMovedToNewProjects);
            }
            filename ??= mediaFile.Filename;
            if (filename != mediaFile.Filename)
            {
                return TypedResults.BadRequest(FileUploadErrorMessage.UploadedFilesCannotBeRenamed);
            }
            if (mediaFile.Metadata is null) mediaFile.Metadata = metadata;
            else mediaFile.Metadata.Merge(metadata);
        }
        var project = await lexBoxDb.Projects.FindAsync(projectId);
        if (project is null) return TypedResults.NotFound();
        var projectFolder = config.Value.GetProjectFolder(project.Code, projectId.Value);
        if (projectFolder is null)
        {
            return TypedResults.BadRequest(FileUploadErrorMessage.ProjectFolderNotFoundInFwHeadless);
        }
        var filePath = Path.Join(projectFolder, filename);
        mediaFile.Metadata.Filename = filename;
        mediaFile.Metadata.SizeInBytes = (int)file.Length;
        var writtenLength = await WriteFileToDisk(filePath, file.OpenReadStream());
        if (writtenLength > maxUploadSize)
        {
            SafeDelete(filePath);
            lexBoxDb.Files.Remove(mediaFile);
            await lexBoxDb.SaveChangesAsync();
            var detail = $"Max upload size is {maxUploadSize.ToString(NumberFormatInfo.InvariantInfo)} bytes, try reducing image quality or downsampling audio";
            return TypedResults.Problem(statusCode: 413, detail: detail);
        }
        if (writtenLength != file.Length)
        {
            // TODO: Log warning about mismatched length?
            mediaFile.Metadata.SizeInBytes = (int)writtenLength;
        }
        await AddEntityTagMetadata(mediaFile, filePath);
        mediaFile.UpdateUpdatedDate();
        await lexBoxDb.SaveChangesAsync();
        // Add ETag to the POST results so uploaders could, in theory, save it and use it later in a GET operation
        var entityTag = mediaFile.Metadata.Sha256Hash;
        httpContext.Response.Headers.ETag = entityTag;
        var responseBody = new PostFileResult(fileId.Value);
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
        // NOTE for when we implement this: temp file should be in same directory with random name, otherwise move operation isn't guaranteed to be atomic on all filesystems
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

    private static void SafeDelete(string filePath)
    {
        // Delete file at path, ignoring all errors such as "file not found"
        try { File.Delete(filePath); }
        catch { }
    }
}
