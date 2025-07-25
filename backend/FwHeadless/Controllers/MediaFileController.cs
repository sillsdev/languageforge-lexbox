using FwHeadless.Models;
using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using Microsoft.Net.Http.Headers;
using System.Globalization;
using FwHeadless.Media;
using MimeMapping;

namespace FwHeadless.Controllers;

public static class MediaFileController
{
    public const string LinkedFiles = "LinkedFiles";

    public static async Task<Results<Ok<FileListing>, NotFound, BadRequest>> ListFiles(
        Guid projectId,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb,
        string relativePath = "")
    {
        var project = await lexBoxDb.Projects.FindAsync(projectId);
        if (project is null) return TypedResults.NotFound();
        var projectFolder = config.Value.GetFwDataProject(project.Code, projectId).ProjectFolder;
        if (!Directory.Exists(projectFolder)) return TypedResults.NotFound();
        // Prevent directory-traversal attacks: no ".." allowed in relativePath
        if (relativePath.Contains(".."))
        {
            // This will also fail for any requests with ".." in the name of a directory, but that's an acceptable loss
            return TypedResults.BadRequest();
        }
        var path = Path.Join(projectFolder, LinkedFiles, relativePath); // Do NOT use Path.Combine here
        var files =
            Directory.EnumerateFiles(path, "*", new EnumerationOptions() { RecurseSubdirectories = true })
                .Select(file => Path.GetRelativePath(path, file))
                .ToArray();
        return TypedResults.Ok(new FileListing(files));
    }

    public static async Task<Results<PhysicalFileHttpResult, NotFound>> GetFile(
        Guid fileId,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb,
        MediaFileService mediaFileService)
    {
        var madeChanges = false;
        var mediaFile = await lexBoxDb.Files.FindAsync(fileId);
        if (mediaFile is null) return TypedResults.NotFound();
        var filePath = mediaFileService.FilePath(mediaFile);
        if (!File.Exists(filePath)) return TypedResults.NotFound();
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
        return TypedResults.PhysicalFile(filePath, contentType, Path.GetFileName(mediaFile.Filename), mediaFile.UpdatedDate, entityTag, enableRangeProcessing: true);
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
        var projectFolder = config.Value.GetFwDataProject(project.Code, projectId).ProjectFolder;
        SafeDeleteMediaFile(mediaFile, projectFolder, lexBoxDb);
        await lexBoxDb.SaveChangesAsync();
        return TypedResults.Ok();
    }

    public static async Task<Results<Ok<PostFileResult>, Created<PostFileResult>, NotFound, BadRequest<FileUploadErrorMessage>, ProblemHttpResult>> PutFile(
        Guid fileId,
        [FromForm] Guid projectId,
        [FromForm] string? filename,
        [FromForm] IFormFile file,
        [FromForm] FileMetadata? metadata,
        [FromForm] string? linkedFilesSubfolderOverride,
        HttpContext httpContext,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb)
    {
        var result = await HandleFileUpload(fileId, projectId, filename, file, metadata, linkedFilesSubfolderOverride, httpContext, config, lexBoxDb, newFilesAllowed: false);
        return result;
    }

    [HttpPost]
    public static async Task<Results<Ok<PostFileResult>, Created<PostFileResult>, NotFound, BadRequest<FileUploadErrorMessage>, ProblemHttpResult>> PostFile(
        [FromQuery] Guid projectId,
        [FromForm] Guid? fileId,
        [FromForm] string? filename,
        [FromForm] IFormFile file,
        [FromForm] FileMetadata? metadata,
        [FromForm] string? linkedFilesSubfolderOverride,
        HttpContext httpContext,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb)
    {
        var result = await HandleFileUpload(fileId, projectId, filename, file, metadata, linkedFilesSubfolderOverride, httpContext, config, lexBoxDb, newFilesAllowed: true);
        return result;
    }

    public static async Task<Results<Ok<PostFileResult>, Created<PostFileResult>, NotFound, BadRequest<FileUploadErrorMessage>, ProblemHttpResult>> HandleFileUpload(
        Guid? fileId,
        Guid projectId,
        string? filename,
        IFormFile file,
        FileMetadata? metadata,
        string? linkedFilesSubfolderOverride,
        HttpContext httpContext,
        IOptions<FwHeadlessConfig> config,
        LexBoxDbContext lexBoxDb,
        bool newFilesAllowed)
    {
        if (CheckUploadSize(file, httpContext, config) is {} result) return result;
        MediaFile? mediaFile;
        bool newFile;
        try
        {
            (mediaFile, newFile) = await CreateOrUpdateMediaFile(lexBoxDb,
                fileId,
                projectId,
                filename,
                file,
                metadata,
                linkedFilesSubfolderOverride,
                newFilesAllowed,
                httpContext,
                config);
        }
        catch (NotFoundException) { return TypedResults.NotFound(); }
        catch (UploadedFilesCannotBeMovedToNewProjects) { return TypedResults.BadRequest(FileUploadErrorMessage.UploadedFilesCannotBeMovedToNewProjects); }
        catch (UploadedFilesCannotBeMovedToDifferentLinkedFilesSubfolders) { return TypedResults.BadRequest(FileUploadErrorMessage.UploadedFilesCannotBeMovedToDifferentLinkedFilesSubfolders); }
        catch (ProjectFolderNotFoundInFwHeadless) { return TypedResults.BadRequest(FileUploadErrorMessage.ProjectFolderNotFoundInFwHeadless); }
        catch (FileTooLarge)
        {
            var detail = $"Max upload size is {config.Value.MaxUploadFileSizeBytes.ToString(NumberFormatInfo.InvariantInfo)} bytes, try reducing image quality or downsampling audio";
            return TypedResults.Problem(statusCode: 413, detail: detail);
        }

        // Add ETag to the POST results so uploaders could, in theory, save it and use it later in a GET operation
        var entityTag = mediaFile.Metadata!.Sha256Hash;
        httpContext.Response.Headers.ETag = $"\"{entityTag}\"";
        var responseBody = new PostFileResult(mediaFile.Id);
        if (newFile)
        {
            var newLocation = $"{Routes.MediaFileRoutes.RootRoute}/{fileId}";
            return TypedResults.Created(newLocation, responseBody);
        }
        else
        {
            return TypedResults.Ok(responseBody);
        }
    }

    private static ProblemHttpResult? CheckUploadSize(IFormFile file, HttpContext httpContext, IOptions<FwHeadlessConfig> config)
    {
        // Sanity check: reject ridiculously large uploads before doing any work
        var maxUploadSize = config.Value.MaxUploadFileSizeBytes;
        if ((httpContext.Request.ContentLength is not null && httpContext.Request.ContentLength > maxUploadSize) || file.Length > maxUploadSize)
        {
            var detail = $"Max upload size is {maxUploadSize.ToString(NumberFormatInfo.InvariantInfo)} bytes, try reducing image quality or downsampling audio";
            return TypedResults.Problem(statusCode: 413, detail: detail);
        }

        return null;
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
        string tempFile = "";
        try
        {
            var dirName = Path.GetDirectoryName(filePath);
            if (dirName is not null) Directory.CreateDirectory(dirName);
            tempFile = Path.Join(dirName, Path.GetRandomFileName());
            await using (var writeStream = File.Open(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
            {
                await contents.CopyToAsync(writeStream);
            }
            File.Move(tempFile, filePath, overwrite: true);
        }
        finally
        {
            // If anything fails, delete temp file
            if (!string.IsNullOrEmpty(tempFile) && File.Exists(tempFile)) SafeDelete(tempFile);
        }
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

    private static readonly string[] KeysToRemove = ["file", "filename", "fileId", "projectId"];

    private static async Task<Dictionary<string, object>> ExtraFieldsFromForm(HttpRequest httpRequest, CancellationToken token = default)
    {
        IFormCollection form;
        try
        {
            form = await httpRequest.ReadFormAsync(token);
        }
        catch
        {
            // Any errors, such as "body is not a form" or cancellation token triggered, and we'll just return an empty dictionary
            return [];
        }
        var extraKeys = form.Keys.Except(KeysToRemove).Where(key => !FileMetadataProperties.IsMetadataProp(key));
        var extraFields = new Dictionary<string, object>();
        foreach (var key in extraKeys)
        {
            if (form.TryGetValue(key, out var values))
            {
                if (values.FirstOrDefault() is string s) extraFields[key] = s;
            }
        }
        return extraFields;
    }

    private static async Task<FileMetadata> InitMetadata(FileMetadata? metadata, IFormFile file, string filename, HttpRequest httpRequest)
    {
        metadata ??= new FileMetadata();
        // HTTP Content-Type header will be "multipart/form-data; bondary=(something)". We want the content-type from the uploaded file, not HTTP
        var mimeType = !string.IsNullOrEmpty(metadata.MimeType) ? metadata.MimeType : file.ContentType;
        // If we have a filename but no mime type, then try to guess it from the filename at this point
        if (string.IsNullOrEmpty(mimeType))
            mimeType = MimeUtility.GetMimeMapping(filename);
        metadata.MimeType = mimeType;
        // Form entries not found in FileMetadata won't automatically be mapped into ExtraFields, we have to do it manually
        metadata.ExtraFields = await ExtraFieldsFromForm(httpRequest);
        return metadata;
    }

    private class NotFoundException : Exception;
    private class UploadedFilesCannotBeMovedToNewProjects : Exception;
    private class UploadedFilesCannotBeMovedToDifferentLinkedFilesSubfolders : Exception;
    private class ProjectFolderNotFoundInFwHeadless : Exception;
    private class FileTooLarge : Exception;
    private static async Task<(MediaFile, bool newFile)> CreateOrUpdateMediaFile(
        LexBoxDbContext lexBoxDb,
        Guid? fileId,
        Guid projectId,
        string? filename,
        IFormFile file,
        FileMetadata? metadata,
        string? subfolderOverride,
        bool newFilesAllowed,
        HttpContext httpContext,
        IOptions<FwHeadlessConfig> config)
    {
        if (fileId is null || fileId.Value == default)
        {
            fileId = Guid.NewGuid();
        }
        var mediaFile = await lexBoxDb.Files.FindAsync(fileId);
        var newFile = mediaFile is null;
        if (mediaFile is null && !newFilesAllowed)
        {
            // PUT requests must modify an existing file and return 404 if it doesn't exist
            throw new NotFoundException();
        }

        // If no filename specified in form, get it from uploaded file
        if (string.IsNullOrEmpty(filename)) filename = file.FileName;
        metadata = await InitMetadata(metadata, file, filename, httpContext.Request);
        if (mediaFile is null)
        {
            var subfolder = subfolderOverride ?? GuessSubfolderFromMimeType(metadata.MimeType) ?? "";
            mediaFile = new MediaFile()
            {
                Id = fileId.Value,
                Filename = Path.Join(LinkedFiles, subfolder, fileId.ToString(), filename),
                ProjectId = projectId,
                Metadata = metadata,
            };
            lexBoxDb.Files.Add(mediaFile);
        }
        else
        {
            if (projectId != mediaFile.ProjectId)
            {
                throw new UploadedFilesCannotBeMovedToNewProjects();
            }
            if (subfolderOverride is not null && !mediaFile.Filename.StartsWith(Path.Join(LinkedFiles, subfolderOverride)))
            {
                throw new UploadedFilesCannotBeMovedToDifferentLinkedFilesSubfolders();
            }
            if (mediaFile.Metadata is null) mediaFile.Metadata = metadata;
            else mediaFile.Metadata.Merge(metadata);
        }

        var project = await lexBoxDb.Projects.FindAsync(projectId);
        if (project is null) throw new NotFoundException();
        var projectFolder = config.Value.GetFwDataProject(project.Code, projectId).ProjectFolder;
        await WriteFileAndUpdateMediaFileMetadata(lexBoxDb, mediaFile, projectFolder, file, config.Value.MaxUploadFileSizeBytes);
        return (mediaFile, newFile);
    }

    private static async Task WriteFileAndUpdateMediaFileMetadata(LexBoxDbContext lexBoxDb, MediaFile mediaFile, string projectFolder, IFormFile file, long maxUploadSize)
    {
        if (!Directory.Exists(projectFolder))
        {
            throw new ProjectFolderNotFoundInFwHeadless();
        }

        var filePath = Path.Join(projectFolder, mediaFile.Filename);
        if (mediaFile.Metadata is not null) mediaFile.Metadata.SizeInBytes = (int)file.Length;
        long writtenLength = 0;
        await using (var readStream = file.OpenReadStream())
        {
            writtenLength = await WriteFileToDisk(filePath, readStream);
        }
        if (writtenLength > maxUploadSize)
        {
            SafeDeleteMediaFile(mediaFile, projectFolder, lexBoxDb);
            await lexBoxDb.SaveChangesAsync();
            throw new FileTooLarge();
        }
        if (writtenLength != file.Length)
        {
            // TODO: Log warning about mismatched length?
            if (mediaFile.Metadata is not null) mediaFile.Metadata.SizeInBytes = (int)writtenLength;
        }
        await AddEntityTagMetadata(mediaFile, filePath);
        mediaFile.UpdateUpdatedDate();
        await lexBoxDb.SaveChangesAsync();
    }

    private static string? GuessSubfolderFromMimeType(string? mimeType)
    {
        if (mimeType is null) return null;
        if (mimeType.StartsWith("image/")) return "Pictures";
        if (mimeType.StartsWith("audio/")) return "AudioVisual";
        if (mimeType.StartsWith("video/")) return "AudioVisual";
        // Special cases
        if (mimeType == "application/mp4") return "AudioVisual"; // Some apps don't want to commit to audio/ or video/, but we don't care which it is
        return null;
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
