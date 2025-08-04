using System.Security.Cryptography;
using LcmCrdt;
using LcmCrdt.MediaServer;
using LexCore.Entities;
using LexCore.Exceptions;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniLcm.Media;
using SIL.LCModel;
using MediaFile = LexCore.Entities.MediaFile;

namespace FwHeadless.Media;

public class MediaFileService(LexBoxDbContext dbContext, IOptions<FwHeadlessConfig> config)
{
    public record MediaFileSyncResult(List<MediaFile> Added, List<MediaFile> Removed);
    // TODO: This assumes FieldWorks is the source of truth, which is not true when FWL starts adding/deleting files
    public async Task<MediaFileSyncResult> SyncMediaFiles(LcmCache cache)
    {
        var result = new MediaFileSyncResult([], []);
        var projectId = config.Value.LexboxProjectId(cache);
        var existingDbFiles = dbContext.Files.Where(p => p.ProjectId == projectId).AsTracking().AsAsyncEnumerable();
        var existingFwFiles = FilesRelativeToHgRepo(cache).ToHashSet();
        await foreach (var mediaFile in existingDbFiles)
        {
            if (existingFwFiles.Remove(mediaFile.Filename))
            {
                //nothing to do, the file exists in the db and in the hg repo
                continue;
            }

            //file has been deleted from hg, so remove it from the db
            dbContext.Files.Remove(mediaFile);
            result.Removed.Add(mediaFile);
        }
        //files not removed are newly created, and we need to record them in the db
        foreach (var newFwFile in existingFwFiles)
        {
            var mediaFile = new MediaFile
            {
                Id = Guid.NewGuid(),
                Filename = newFwFile,
                ProjectId = projectId,
                Metadata = new FileMetadata
                {
                    MimeType = MimeMapping.MimeUtility.GetMimeMapping(newFwFile),
                    SizeInBytes = (int)new FileInfo(Path.Join(cache.ProjectId.ProjectFolder, newFwFile)).Length,
                }
            };
            dbContext.Files.Add(mediaFile);
            result.Added.Add(mediaFile);
        }

        await dbContext.SaveChangesAsync();
        return result;
    }

    private IEnumerable<string> FilesRelativeToHgRepo(LcmCache cache)
    {
        if (!Directory.Exists(cache.LangProject.LinkedFilesRootDir)) yield break;
        foreach (var file in Directory.EnumerateFiles(cache.LangProject.LinkedFilesRootDir, "*", SearchOption.AllDirectories))
        {
            yield return Path.GetRelativePath(cache.ProjectId.ProjectFolder, file);
        }
    }

    /// <summary>
    /// find a media file based on the path and project
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="path">full file path to find the file at</param>
    /// <returns></returns>
    /// <exception cref="NotFoundException">Throws when it can't find the file</exception>
    public MediaFile FindMediaFile(Guid projectId, string path)
    {
        if (!Path.IsPathRooted(path))
        {
            throw new ArgumentException("Path must be absolute", nameof(path));
        }
        var fwDataFolder = config.Value.GetFwDataFolder(config.Value.GetProjectFolder(projectId));
        if (!path.StartsWith(fwDataFolder))
        {
            throw new ArgumentException("Path must be in the project storage root", nameof(path));
        }

        path = Path.GetRelativePath(fwDataFolder, path);
        return dbContext.Files.FirstOrDefault(f => f.ProjectId == projectId && f.Filename == path) ??
               throw new NotFoundException(
                   $"Unable to find file {path}, in project {projectId}.",
                   nameof(MediaFile));
    }

    public MediaFile? FindMediaFile(Guid fileId)
    {
        return dbContext.Files.Find(fileId);
    }

    public async ValueTask<MediaFile?> FindMediaFileAsync(Guid fileId)
    {
        return await dbContext.Files.FindAsync(fileId);
    }

    public string FilePath(MediaFile mediaFile)
    {
        return Path.Join(config.Value.GetFwDataFolder(mediaFile.ProjectId), mediaFile.Filename);
    }

    public async Task SyncMediaFiles(Guid projectId, LcmMediaService lcmMediaService)
    {
        var lcmResources = (await lcmMediaService.AllResources()).ToDictionary(r => r.Id);
        var existingDbFiles = dbContext.Files.Where(p => p.ProjectId == projectId).AsAsyncEnumerable();
        await foreach (var existingDbFile in existingDbFiles)
        {
            if (lcmResources.Remove(existingDbFile.Id))
            {
                //nothing to do, the file was already tracked in harmony
                continue;
            }
            await lcmMediaService.AddExistingRemoteResource(existingDbFile.Id, FilePath(existingDbFile));
        }
        foreach (var lcmResource in lcmResources.Values)
        {
            await lcmMediaService.DeleteResource(lcmResource.Id);
        }
    }

    public async Task SaveMediaFile(MediaFile mediaFile, Stream fileStream)
    {
        var fwDataFolder = config.Value.GetFwDataFolder(mediaFile.ProjectId);
        if (!Directory.Exists(fwDataFolder)) throw new ProjectFolderNotFoundInFwHeadless();
        var entry = dbContext.Entry(mediaFile);
        if (entry.State == EntityState.Detached) entry.State = EntityState.Added;

        var filePath = Path.Join(fwDataFolder, mediaFile.Filename);
        var dirName = Path.GetDirectoryName(filePath);
        if (dirName is not null) Directory.CreateDirectory(dirName);
        var tempFile = Path.Join(dirName, Path.GetRandomFileName());
        try
        {
            await using (var writeStream = File.Open(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
            {
                await fileStream.CopyToAsync(writeStream);
            }

            File.Move(tempFile, filePath, overwrite: true);
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempFile) && File.Exists(tempFile)) File.Delete(tempFile);
        }

        var fileInfo = new FileInfo(filePath);
        var fileLength = fileInfo.Length;
        if (fileLength > config.Value.MaxUploadFileSizeBytes)
        {
            await DeleteMediaFile(mediaFile);
            throw new FileTooLarge();
        }

        //todo write hg commit

        mediaFile.InitializeMetadataIfNeeded(filePath);
        mediaFile.Metadata.SizeInBytes = (int)fileLength;
        mediaFile.Metadata.Sha256Hash = await Sha256OfFile(filePath);

        mediaFile.UpdateUpdatedDate();
        await dbContext.SaveChangesAsync();

    }

    public async Task DeleteMediaFile(MediaFile mediaFile)
    {
        var filePath = Path.Join(config.Value.GetFwDataFolder(mediaFile.ProjectId), mediaFile.Filename);
        SafeDelete(filePath);
        var dirPath = Path.GetDirectoryName(filePath);
        if (dirPath?.EndsWith(mediaFile.Id.ToString()) == true)
            SafeDeleteDirectory(dirPath); // Will not delete dir if not empty, but that's OK
        dbContext.Files.Remove(mediaFile);
        await dbContext.SaveChangesAsync();
    }

    public static async Task<string> Sha256OfFile(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexStringLower(hash);
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
}
