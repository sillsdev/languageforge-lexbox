using FwHeadless.Models;
using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using Microsoft.Net.Http.Headers;
using System.Globalization;
using System.Text.Json;
using MimeMapping;

namespace FwHeadless.Controllers;

public static class MediaFileController
{
    public static async Task<Results<Ok<FileListing>, NotFound>> ListFiles(
        Guid projectId,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb,
        string relativePath = "")
    {
        var project = await lexBoxDb.Projects.FindAsync(projectId);
        if (project is null) return TypedResults.NotFound();
        var projectFolder = config.Value.GetProjectFolder(project.Code, projectId);
        var path = Path.Join(projectFolder, relativePath); // Do NOT use Path.Combine here
        var files =
            Directory.EnumerateFiles(path, "*", new EnumerationOptions() { RecurseSubdirectories = true })
                .Select(file => Path.GetRelativePath(projectFolder, file))
                .ToArray();
        return TypedResults.Ok(new FileListing(files));
    }

    public static async Task<Results<PhysicalFileHttpResult, NotFound>> GetFile(
        Guid fileId,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb)
    {
        var madeChanges = false;
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
            contentType = MimeUtility.GetMimeMapping(filePath);
            mediaFile.Metadata.MimeType = contentType;
            madeChanges = true;
        }
        madeChanges = await AddEntityTagMetadataIfNotPresent(mediaFile, filePath) || madeChanges;
        if (madeChanges) await lexBoxDb.SaveChangesAsync();
        var entityTag = new EntityTagHeaderValue($"\"{mediaFile.Metadata.Sha256Hash!}\"");
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
        SafeDeleteMediaFile(mediaFile, projectFolder, lexBoxDb);
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
        var result = await HandleFileUpload(fileId, projectId, filename, file, metadata, httpContext, config, lexBoxDb, newFilesAllowed: false);
        return result;
    }

    [HttpPost]
    public static async Task<Results<Ok<PostFileResult>, Created<PostFileResult>, NotFound, BadRequest<FileUploadErrorMessage>, ProblemHttpResult>> PostFile(
        [FromQuery] Guid projectId,
        [FromForm] Guid? fileId,
        [FromForm] string? filename,
        [FromForm] IFormFile file,
        [FromForm] FileMetadata? metadata,
        HttpContext httpContext,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb)
    {
        var result = await HandleFileUpload(fileId, projectId, filename, file, metadata, httpContext, config, lexBoxDb, newFilesAllowed: true);
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
        bool newFilesAllowed)
    {
        var replacedExistingFile = false;
        // Sanity check: reject ridiculously large uploads before doing any work
        var maxUploadSize = config.Value.MaxUploadFileSizeBytes;
        if ((httpContext.Request.ContentLength is not null && httpContext.Request.ContentLength > maxUploadSize) || file.Length > maxUploadSize)
        {
            var detail = $"Max upload size is {maxUploadSize.ToString(NumberFormatInfo.InvariantInfo)} bytes, try reducing image quality or downsampling audio";
            return TypedResults.Problem(statusCode: 413, detail: detail);
        }
        // HTTP Content-Type header will be "multipart/form-data; bondary=(something)". We want the content-type from the uploaded file, not HTTP
        var mimeType = file.ContentType;
        // If no filename specified in form, get it from uploaded file
        if (string.IsNullOrEmpty(filename)) filename = file.FileName;
        // If *still* no filename, then use `file.ext` where the extension is calculated from the Content-Type header, defaulting to `.bin` if not provided
        if (string.IsNullOrEmpty(filename))
        {
            var ext = MimeUtility.GetExtensions(mimeType ?? "")?.FirstOrDefault() ?? "bin";
            filename = $"file.{ext}";
        }
        // If we have a filename but no mime type, then try to guess it from the filename at this point
        if (string.IsNullOrEmpty(mimeType)) mimeType = MimeUtility.GetMimeMapping(filename);
        if (fileId is null || fileId.Value == default)
        {
            fileId = Guid.NewGuid();
        }
        metadata ??= new FileMetadata();
        if (string.IsNullOrEmpty(metadata.MimeType)) metadata.MimeType = mimeType;
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
            // If no filename specified in form, get it from uploaded file
            if (string.IsNullOrEmpty(filename)) filename = file.FileName;
            mediaFile = new MediaFile()
            {
                Id = fileId.Value,
                Filename = Path.Join(fileId.Value.ToString(), filename),
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
            filename = Path.GetFileName(mediaFile.Filename); // Strip GUID prefix for consistency with create-file path; we'll be adding it back later
            if (mediaFile.Metadata is null) mediaFile.Metadata = metadata;
            else mediaFile.Metadata.Merge(metadata);
        }
        var project = await lexBoxDb.Projects.FindAsync(projectId);
        if (project is null) return TypedResults.NotFound();
        var projectFolder = config.Value.GetProjectFolder(project.Code, projectId.Value);
        if (projectFolder is null || !Directory.Exists(projectFolder))
        {
            return TypedResults.BadRequest(FileUploadErrorMessage.ProjectFolderNotFoundInFwHeadless);
        }
        var fileFolder = Path.Join(projectFolder, fileId.Value.ToString());
        Directory.CreateDirectory(fileFolder);
        var filePath = Path.Join(fileFolder, filename);
        mediaFile.Metadata.SizeInBytes = (int)file.Length;
        long writtenLength = 0;
        await using (var readStream = file.OpenReadStream())
        {
            writtenLength = await WriteFileToDisk(filePath, readStream);
        }
        if (writtenLength > maxUploadSize)
        {
            SafeDeleteMediaFile(mediaFile, projectFolder, lexBoxDb);
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
        httpContext.Response.Headers.ETag = $"\"{entityTag}\"";
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

    private static async Task<long> WriteFileToDisk(string filePath, Stream contents)
    {
        if (contents is null) return 0;
        long startPosition = 0;
        try
        {
            startPosition = contents.Position;
        }
        catch { }
        // First write to temp file, then move file into place, overwriting existing file
        // That way files will be replaced atomically, and a failure halfway through the process won't result in the existing file being lost
        var tempFile = Path.Join(Path.GetDirectoryName(filePath), Path.GetRandomFileName());
        await using (var writeStream = File.Open(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
        {
            await contents.CopyToAsync(writeStream);
        }
        File.Move(tempFile, filePath, overwrite: true);
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
        await using var stream = File.OpenRead(filePath);
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

    private static void SafeDeleteDirectory(string dirPath, bool recursive = false)
    {
        // Delete file at path, ignoring all errors such as "directory not empty"
        try { Directory.Delete(dirPath, recursive); }
        catch { }
    }

    private static void SafeDeleteMediaFile(MediaFile mediaFile, string projectFolder, LexBoxDbContext lexBoxDb)
    {
        var filePath = Path.Join(projectFolder, mediaFile.Filename);
        SafeDelete(filePath);
        var dirPath = Path.Join(projectFolder, mediaFile.Id.ToString());
        SafeDeleteDirectory(dirPath); // Will not delete dir if not empty, but that's OK
        lexBoxDb.Files.Remove(mediaFile);
    }
}
