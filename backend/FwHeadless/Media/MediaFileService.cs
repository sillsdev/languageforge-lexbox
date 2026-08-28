using System.Net.Mime;
using System.Security.Cryptography;
using FwHeadless.Controllers;
using FwHeadless.Services;
using LcmCrdt.MediaServer;
using LexCore.Entities;
using LexCore.Exceptions;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniLcm.Media;
using SIL.LCModel;
using FileMetadata = LexCore.Entities.FileMetadata;
using MediaFile = LexCore.Entities.MediaFile;

namespace FwHeadless.Media;

public class MediaFileService(LexBoxDbContext dbContext, IOptions<FwHeadlessConfig> config, ISendReceiveService sendReceiveService)
{
    public record MediaFileSyncResult(List<MediaFile> Added, List<MediaFile> Removed);

    public virtual async Task<MediaFileSyncResult> SyncMediaFiles(LcmCache cache)
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

            //a pending row (revision 0) legitimately has no physical hg file yet (it reserves the anticipated
            //path for a binary that hasn't been uploaded), so don't mistake it for a hg-side deletion and remove it.
            if (mediaFile.Revision == 0) continue;

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
                Revision = 1, // a file discovered in hg is a real backed file, not a pending (0) reservation
                Metadata = new FileMetadata
                {
                    MimeType = MimeMapping.MimeUtility.GetMimeMapping(newFwFile),
                    SizeInBytes = new FileInfo(Path.Join(cache.ProjectId.ProjectFolder, newFwFile)).Length,
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
    /// Non-throwing lookup of a media file by its absolute path within the project. Returns null when no row
    /// matches (including a path outside the project storage root), so callers can resolve a path that may or
    /// may not have a Files row — including a pending row whose binary isn't on disk yet. Use
    /// <see cref="GetMediaFile(Guid, string)"/> when a missing row should be an error.
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="path">absolute path within the project storage root</param>
    public MediaFile? FindMediaFile(Guid projectId, string path)
    {
        if (!Path.IsPathRooted(path)) throw new ArgumentException("Path must be absolute", nameof(path));
        var fwDataFolder = config.Value.GetFwDataFolder(config.Value.GetProjectFolder(projectId));
        if (!path.StartsWith(fwDataFolder)) return null;
        path = Path.GetRelativePath(fwDataFolder, path);
        return dbContext.Files.FirstOrDefault(f => f.ProjectId == projectId && f.Filename == path);
    }

    /// <summary>
    /// Look up a media file by its absolute path, throwing when none exists. Thin throwing wrapper over
    /// <see cref="FindMediaFile(Guid, string)"/>.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when no matching file is found.</exception>
    public MediaFile GetMediaFile(Guid projectId, string path)
    {
        return FindMediaFile(projectId, path) ??
               throw new NotFoundException($"Unable to find file {path}, in project {projectId}.", nameof(MediaFile));
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

    public virtual async Task SyncMediaFiles(Guid projectId, LcmMediaService lcmMediaService)
    {
        var lcmResources = (await lcmMediaService.AllResources()).ToDictionary(r => r.Id);
        var existingDbFiles = dbContext.Files.Where(p => p.ProjectId == projectId).AsAsyncEnumerable();
        var dbChanged = false;
        await foreach (var existingDbFile in existingDbFiles)
        {
            if (lcmResources.Remove(existingDbFile.Id, out var lcmResource))
            {
                //the file was already tracked in harmony, but the metadata is missing, so add it
                if (lcmResource.Metadata is null)
                    await lcmMediaService.AddMissingMetadata(lcmResource, ToLcmFileMetadata(existingDbFile));
                //nothing to do, the file was already tracked in harmony
                continue;
            }

            // No live Harmony resource matches this Files row.
            if (existingDbFile.Revision == 0)
            {
                // A pending row reserves the anticipated path for a resource that is now gone. Reclaim it — and
                // skip AddExistingRemoteResource below, which would throw FileNotFoundException (a pending row has
                // no binary on disk). Only reached when the resource is absent, so there is no reserve->delete loop.
                dbContext.Files.Remove(existingDbFile);
                dbChanged = true;
                continue;
            }

            // A backed (uploaded) file lexbox knows about but Harmony doesn't yet — track it.
            await lcmMediaService.AddExistingRemoteResource(existingDbFile.Id, FilePath(existingDbFile), ToLcmFileMetadata(existingDbFile));
        }
        foreach (var lcmResource in lcmResources.Values)
        {
            // A resource with no matching Files row that was never uploaded (RemoteId == null / !Remote) is a
            // pending upload: its binary referenced by an entry but not yet on the server. Instead of skipping
            // (which leaves the media unresolvable and forces the sync layer to compensate), create a *pending*
            // Files row that reserves the anticipated path. The row makes the media URI resolve, so the normal
            // CRDT->FwData write records the anticipated path in FwData; once the binary is uploaded to that
            // same path (PutFile keeps a pre-existing row's Filename), the revision advances to 1 and the link
            // self-heals. Next sync the resource matches this row in the loop above and is removed from the
            // leftovers, so there is no risk of creating a second pending row.
            if (!lcmResource.Remote)
            {
                var metadata = lcmResource.Metadata;
                // Without a filename we can't reserve a path, so leave the resource untouched (old behavior).
                if (string.IsNullOrEmpty(metadata?.Filename)) continue;

                var subfolder = MediaFileController.GuessSubfolderFromMimeType(metadata.MimeType) ?? "";
                var pendingRow = new MediaFile
                {
                    Id = lcmResource.Id,
                    ProjectId = projectId,
                    Filename = Path.Join(MediaFileController.LinkedFiles, subfolder, lcmResource.Id.ToString(), metadata.Filename),
                    Metadata = FromLcmFileMetadata(metadata),
                    Revision = 0, // pending: no binary uploaded yet
                };
                dbContext.Files.Add(pendingRow);
                dbChanged = true;
                continue;
            }
            await lcmMediaService.DeleteResource(lcmResource.Id);
        }

        if (dbChanged) await dbContext.SaveChangesAsync();
    }

    private static FileMetadata FromLcmFileMetadata(LcmFileMetadata metadata)
    {
        return new FileMetadata
        {
            MimeType = metadata.MimeType,
            Author = metadata.Author,
            UploadDate = metadata.UploadDate,
            SizeInBytes = metadata.SizeInBytes,
            ExtraFields = metadata.ExtraFields.ToDictionary(),
        };
    }

    private static LcmFileMetadata ToLcmFileMetadata(MediaFile existingDbFile)
    {

        return new LcmFileMetadata(existingDbFile.Filename, existingDbFile.Metadata?.MimeType ?? MediaTypeNames
                .Application.Octet, existingDbFile.Metadata?.Author, existingDbFile.Metadata?.UploadDate,
            existingDbFile.Metadata?.SizeInBytes)
        {
            ExtraFields = existingDbFile.Metadata?.ExtraFields.ToDictionary() ?? new Dictionary<string, object>(),
        };
    }

    public async Task SaveMediaFile(MediaFile mediaFile, Stream fileStream)
    {
        if ((fileStream.SafeLength() ?? 0) > config.Value.MaxUploadFileSizeBytes)
        {
            throw new FileTooLarge();
        }
        var fwDataFolder = config.Value.GetFwDataFolder(mediaFile.ProjectId);
        if (!Directory.Exists(fwDataFolder)) throw new ProjectFolderNotFoundInFwHeadless();
        var entry = dbContext.Entry(mediaFile);
        if (entry.State == EntityState.Detached) entry.State = EntityState.Added;

        var filePath = FilePath(mediaFile);
        var dirName = Path.GetDirectoryName(filePath);
        if (dirName is not null) Directory.CreateDirectory(dirName);
        var tempFile = Path.Join(dirName, Path.GetRandomFileName());
        long fileLength;
        try
        {
            await using (var writeStream = File.Open(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
            {
                await fileStream.CopyToAsync(writeStream);
            }

            fileLength = new FileInfo(tempFile).Length;
            if (fileLength > config.Value.MaxUploadFileSizeBytes)
            {
                await DeleteMediaFile(mediaFile, commitDelete: false);
                throw new FileTooLarge();
            }

            File.Move(tempFile, filePath, overwrite: true);
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempFile) && File.Exists(tempFile)) File.Delete(tempFile);
        }


        //commit the file to hg, otherwise a rollback caused by a merge conflict during S&R will delete the file
        await sendReceiveService.CommitFile(filePath, $"Uploaded file {Path.GetFileName(filePath)}");

        mediaFile.InitializeMetadataIfNeeded(filePath);
        mediaFile.Metadata.SizeInBytes = fileLength;
        mediaFile.Metadata.Sha256Hash = await Sha256OfFile(filePath);

        //the binary is now on disk (and committed to hg): advance the revision. A pending row (0) becomes 1 on
        //its first upload and a normal, backed file; a replacement of an existing file bumps it again (2, 3, …).
        mediaFile.Revision++;

        mediaFile.UpdateUpdatedDate();
        await dbContext.SaveChangesAsync();

    }

    public async Task DeleteMediaFile(MediaFile mediaFile)
    {
        await DeleteMediaFile(mediaFile, commitDelete: true);
    }

    private async Task DeleteMediaFile(MediaFile mediaFile, bool commitDelete)
    {
        var filePath = FilePath(mediaFile);
        var fileExisted = File.Exists(filePath);
        SafeDelete(filePath);
        var dirPath = Path.GetDirectoryName(filePath);
        if (dirPath?.EndsWith(mediaFile.Id.ToString()) == true)
            SafeDeleteDirectory(dirPath); // Will not delete dir if not empty, but that's OK
        if (fileExisted && commitDelete)
        {
            await sendReceiveService.CommitFile(filePath, $"Deleted file {mediaFile.Filename}");
        }
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
