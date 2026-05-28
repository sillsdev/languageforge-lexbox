using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MiniLcm.Media;
using SIL.LCModel;
using UUIDNext;

namespace FwDataMiniLcmBridge.Media;

public class LocalMediaAdapter(IMemoryCache memoryCache, ILogger<LocalMediaAdapter> logger) : IMediaAdapter
{
    //probably don't change this
    private static readonly Guid LocalMediaNamespace = new("45e563a3-f5a6-4d7a-9722-8d7d4d3adfa2");
    private const string LocalMediaAuthority = "localhost";

    private Dictionary<Guid, string> Paths(LcmCache cache)
    {
        return memoryCache.GetOrCreate("LocalMediaPath|" + cache.ProjectId.ProjectFolder,
            entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                return BuildPathsDictionary(cache.LangProject.LinkedFilesRootDir, logger);
            }) ?? throw new Exception("Failed to get paths");
    }

    internal static Dictionary<Guid, string> BuildPathsDictionary(string root, ILogger logger)
    {
        var paths = new Dictionary<Guid, string>();
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var fileId = PathToUri(file).FileId;
            if (paths.TryGetValue(fileId, out var existing))
            {
                // duplicates are possible, because UUIDNext.NewNameBased normalises unicode before hashing
                // keep the NFD path: FW only ever refers to audio via NFD names
                var kept = PreferNfd(existing, file);
                var skipped = ReferenceEquals(kept, existing) ? file : existing;
                paths[fileId] = kept;
                logger.LogWarning("Duplicate media FileId {FileId} in {Root}: kept {Kept}, skipped {Skipped}",
                    fileId, root, kept, skipped);
            }
            else
            {
                paths[fileId] = file;
            }
        }
        return paths;
    }

    private static string PreferNfd(string curr, string @new)
    {
        // only replace curr if new is a strict NFD improvement; otherwise leave the cache stable
        if (Path.GetFileName(curr).IsNormalized(NormalizationForm.FormD)) return curr;
        return Path.GetFileName(@new).IsNormalized(NormalizationForm.FormD) ? @new : curr;
    }

    //path is expected to be relative to the LinkedFilesRootDir
    public MediaUri MediaUriFromPath(string path, LcmCache cache)
    {
        EnsureCorrectRootFolder(path, cache);
        if (!File.Exists(path)) return MediaUri.NotFound;
        var uri = PathToUri(path);
        //this may be a new file, so we need to add it to the cache
        Paths(cache)[uri.FileId] = path;
        return uri;
    }

    private void EnsureCorrectRootFolder(string path, LcmCache cache)
    {
        if (Path.IsPathRooted(path))
        {
            if (path.StartsWith(cache.LangProject.LinkedFilesRootDir)) return;
            throw new ArgumentException("Path must be in the LinkedFilesRootDir", nameof(path));
        }

        throw new ArgumentException("Path must be absolute, " + path, nameof(path));
    }

    private static MediaUri PathToUri(string path)
    {
        return new MediaUri(NewGuidV5(path), LocalMediaAuthority);
    }

    public string? PathFromMediaUri(MediaUri mediaUri, LcmCache cache)
    {
        var paths = Paths(cache);
        if (mediaUri.Authority != LocalMediaAuthority) throw new ArgumentException("MediaUri must be local", nameof(mediaUri));
        if (paths.TryGetValue(mediaUri.FileId, out var path))
        {
            return path;
        }

        return null;
    }

    // produces the same Guid for the same input name
    internal static Guid NewGuidV5(string name)
    {
        return Uuid.NewNameBased(LocalMediaNamespace, name);
    }
}
