using LexCore.Entities;
using LexCore.Exceptions;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SIL.LCModel;

namespace FwHeadless.Media;

public class MediaFileService(LexBoxDbContext dbContext, IOptions<FwHeadlessConfig> config)
{
    public async Task SyncMediaFiles(LcmCache cache)
    {
        var projectId = config.Value.LexboxProjectId(cache);
        Queue<MediaFile> existingDbFiles = new(await dbContext.Files.Where(p => p.ProjectId == projectId).AsTracking().ToArrayAsync());
        var existingFwFiles = FilesRelativeToHgRepo(cache).ToHashSet();
        while (existingDbFiles.Count > 0)
        {
            var mediaFile = existingDbFiles.Dequeue();
            if (existingFwFiles.Remove(mediaFile.Filename))
            {
                //nothing to do, the file exists in the db and in the hg repo
                continue;
            }

            //file has been deleted from hg, so remove it from the db
            dbContext.Files.Remove(mediaFile);
        }
        //files not removed are newly created, and we need to record them in the db
        foreach (var newFwFile in existingFwFiles)
        {
            dbContext.Files.Add(new MediaFile
            {
                Id = Guid.NewGuid(),
                Filename = newFwFile,
                ProjectId = projectId,
                Metadata = new FileMetadata
                {
                    MimeType = MimeMapping.MimeUtility.GetMimeMapping(newFwFile),
                    SizeInBytes = (int)new FileInfo(Path.Join(cache.ProjectId.ProjectFolder, newFwFile)).Length,
                }
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private IEnumerable<string> FilesRelativeToHgRepo(LcmCache cache)
    {
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

    public MediaFile FindMediaFile(Guid fileId)
    {
        return dbContext.Files.Find(fileId) ??
               throw new NotFoundException($"Unable to find file {fileId}.", nameof(MediaFile));
    }
}
