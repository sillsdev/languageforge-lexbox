using System.Collections.Concurrent;
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
        return memoryCache.GetOrCreate(PathsCacheKey(cache),
            entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                return BuildPathsDictionary(cache.LangProject.LinkedFilesRootDir, logger);
            }) ?? throw new Exception("Failed to get paths");
    }

    internal static string PathsCacheKey(LcmCache cache) => "LocalMediaPath|" + cache.ProjectId.ProjectFolder;

    // Static: the adapter can be scoped while the memory cache is a singleton.
    private static readonly Lock UnscannedPathsLock = new();

    // Paths the LinkedFiles scan won't reproduce (pictures FLEx linked in place, casing unlike the disk). Nothing rebuilds them, so no expiry.
    // GetOrCreate can run its factory twice under concurrency, which would drop a dictionary and its mappings, hence the lock.
    private ConcurrentDictionary<Guid, string> UnscannedPaths(LcmCache cache)
    {
        lock (UnscannedPathsLock)
        {
            return memoryCache.GetOrCreate("LocalMediaUnscannedPath|" + cache.ProjectId.ProjectFolder,
                _ => new ConcurrentDictionary<Guid, string>()) ?? throw new Exception("Failed to get unscanned paths");
        }
    }

    internal static Dictionary<Guid, string> BuildPathsDictionary(string root, ILogger logger)
    {
        var paths = new Dictionary<Guid, string>();
        if (!Directory.Exists(root)) return paths;
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

    public MediaUri MediaUriFromPath(string path, LcmCache cache)
    {
        if (!Path.IsPathRooted(path)) throw new ArgumentException("Path must be absolute, " + path, nameof(path));
        if (!File.Exists(path)) return MediaUri.NotFound;
        var uri = PathToUri(path);
        if (!Paths(cache).ContainsKey(uri.FileId)) UnscannedPaths(cache)[uri.FileId] = path;
        return uri;
    }

    private static MediaUri PathToUri(string path)
    {
        return new MediaUri(NewGuidV5(path), LocalMediaAuthority);
    }

    public string? PathFromMediaUri(MediaUri mediaUri, LcmCache cache)
    {
        if (mediaUri.Authority != LocalMediaAuthority) throw new ArgumentException("MediaUri must be local", nameof(mediaUri));
        if (Paths(cache).TryGetValue(mediaUri.FileId, out var path) || UnscannedPaths(cache).TryGetValue(mediaUri.FileId, out path))
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
