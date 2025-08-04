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
using LexCore.Exceptions;
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
        MediaFileService mediaFileService,
        LexBoxDbContext lexBoxDb)
    {
        var mediaFile = await lexBoxDb.Files.FindAsync(fileId);
        if (mediaFile is null) return TypedResults.NotFound();
        var projectId = mediaFile.ProjectId;
        var project = await lexBoxDb.Projects.FindAsync(projectId);
        if (project is null) return TypedResults.NotFound();
        await mediaFileService.DeleteMediaFile(mediaFile);
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
        MediaFileService mediaFileService,
        LexBoxDbContext lexBoxDb)
    {
        var result = await HandleFileUpload(fileId, projectId, filename, file, metadata, linkedFilesSubfolderOverride, httpContext, config, lexBoxDb, mediaFileService, newFilesAllowed: false);
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
        MediaFileService mediaFileService,
        LexBoxDbContext lexBoxDb)
    {
        var result = await HandleFileUpload(fileId, projectId, filename, file, metadata, linkedFilesSubfolderOverride, httpContext, config, lexBoxDb, mediaFileService, newFilesAllowed: true);
        return result;
    }

    public static async
        Task<Results<Ok<PostFileResult>, Created<PostFileResult>, NotFound, BadRequest<FileUploadErrorMessage>,
            ProblemHttpResult>> HandleFileUpload(Guid? fileId,
            Guid projectId,
            string? filename,
            IFormFile file,
            FileMetadata? metadata,
            string? linkedFilesSubfolderOverride,
            HttpContext httpContext,
            IOptions<FwHeadlessConfig> config,
            LexBoxDbContext lexBoxDb,
            MediaFileService mediaFileService,
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
                mediaFileService);
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
            mediaFile.InitializeMetadataIfNeeded(filePath);
            mediaFile.Metadata.Sha256Hash = await MediaFileService.Sha256OfFile(filePath);
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

    private static async Task<(MediaFile, bool newFile)> CreateOrUpdateMediaFile(LexBoxDbContext lexBoxDb,
        Guid? fileId,
        Guid projectId,
        string? filename,
        IFormFile file,
        FileMetadata? metadata,
        string? subfolderOverride,
        bool newFilesAllowed,
        HttpContext httpContext,
        MediaFileService mediaFileService)
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
            throw NotFoundException.ForType<MediaFile>();
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
        if (project is null) throw NotFoundException.ForType<Project>();
        await SaveMediaFile(mediaFileService, mediaFile, file);
        return (mediaFile, newFile);
    }

    private static async Task SaveMediaFile(MediaFileService mediaFileService,
        MediaFile mediaFile,
        IFormFile file)
    {
        await using var readStream = file.OpenReadStream();
        await mediaFileService.SaveMediaFile(mediaFile, readStream);
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
}
