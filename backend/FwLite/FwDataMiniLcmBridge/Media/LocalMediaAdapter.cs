using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.Exceptions;
using MiniLcm.Media;
using SIL.LCModel;
using UUIDNext;

namespace FwDataMiniLcmBridge.Media;

public class LocalMediaAdapter(IMemoryCache memoryCache, ILogger<LocalMediaAdapter> logger) : IMediaAdapter
{
    //probably don't change this
    private static readonly Guid LocalMediaNamespace = new Guid("45e563a3-f5a6-4d7a-9722-8d7d4d3adfa2");
    private const string LocalMediaAuthority = "localhost";

    private Dictionary<Guid, string> Paths(LcmCache cache)
    {
        return memoryCache.GetOrCreate<Dictionary<Guid, string>>("LocalMediaPath|" + cache.ProjectId.ProjectFolder,
            entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                return BuildPathsDictionary(cache.LangProject.LinkedFilesRootDir, logger);
            }) ?? throw new Exception("Failed to get paths");
    }

    // First-wins on duplicate FileId. Two real causes seen in the wild:
    //  - the same path emitted twice by FindNextFile mid-rename (cloud-sync providers),
    //  - NFC + NFD twins of the same audio name (e.g. an NFD süülda.wav from a macOS hg peer
    //    alongside its NFC twin): UUIDNext.NewNameBased normalises before hashing, so twins share
    //    a FileId. FW stores audio refs as NFD so any lookup FW makes still hits the surviving entry.
    internal static Dictionary<Guid, string> BuildPathsDictionary(string root, ILogger logger)
    {
        var paths = new Dictionary<Guid, string>();
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var fileId = PathToUri(file).FileId;
            if (!paths.TryAdd(fileId, file))
            {
                logger.LogWarning("Duplicate media FileId {FileId} in {Root}: kept {Existing}, skipped {Skipped}",
                    fileId, root, paths[fileId], file);
            }
        }
        return paths;
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
