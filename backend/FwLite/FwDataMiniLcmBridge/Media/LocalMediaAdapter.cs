using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using MiniLcm.Media;
using SIL.LCModel;
using SIL.LCModel.Utils;
using UUIDNext;

namespace FwDataMiniLcmBridge.Media;

public class LocalMediaAdapter(ILogger<LocalMediaAdapter> logger) : IMediaAdapter
{
    //probably don't change this
    private static readonly Guid LocalMediaNamespace = new("45e563a3-f5a6-4d7a-9722-8d7d4d3adfa2");
    private const string LocalMediaAuthority = "localhost";

    // Weak keys: a closed project's paths go with its cache. Seeded from a LinkedFiles scan, then MediaUriFromPath adds the paths
    // the scan won't reproduce (pictures FLEx linked in place, casing unlike the disk).
    private readonly ConditionalWeakTable<LcmCache, ConcurrentDictionary<Guid, string>> _paths = new();

    private ConcurrentDictionary<Guid, string> Paths(LcmCache cache)
    {
        return _paths.GetValue(cache, c => new(BuildPathsDictionary(c.LangProject.LinkedFilesRootDir, logger)));
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
                // NFC/NFD twins on disk share a FileId (see NewGuidV5); keep the NFD one, the spelling FW refers to
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
        // LCM hands us NFD references, but FLEx names files NFC (older versions NFD). FLEx finds them with this:
        // https://github.com/sillsdev/liblcm/blob/da7140f07b92/src/SIL.LCModel.Utils/FileUtils.cs#L434
        path = FileUtils.ActualFilePath(path);
        if (!File.Exists(path)) return MediaUri.NotFound;
        var uri = PathToUri(path);
        Paths(cache).TryAdd(uri.FileId, path);
        return uri;
    }

    private static MediaUri PathToUri(string path)
    {
        return new MediaUri(NewGuidV5(path), LocalMediaAuthority);
    }

    public string? PathFromMediaUri(MediaUri mediaUri, LcmCache cache)
    {
        if (mediaUri.Authority != LocalMediaAuthority) throw new ArgumentException("MediaUri must be local", nameof(mediaUri));
        return Paths(cache).GetValueOrDefault(mediaUri.FileId);
    }

    // NewNameBased normalizes before hashing, so NFC and NFD spellings of a path share a FileId. That's safe: LCM stores
    // every media reference as NFD (CmFile.InternalPath, MultiAccessor.set_String and the XML loader all normalize),
    // so a project can't reference both spellings.
    internal static Guid NewGuidV5(string name)
    {
        return Uuid.NewNameBased(LocalMediaNamespace, name);
    }
}
