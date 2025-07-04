using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using MiniLcm;
using MiniLcm.Exceptions;
using SIL.LCModel;
using UUIDNext;

namespace FwDataMiniLcmBridge.Media;

public class LocalMediaAdapter(IMemoryCache memoryCache) : IMediaAdapter
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
                return Directory
                    .EnumerateFiles(cache.LangProject.LinkedFilesRootDir, "*", SearchOption.AllDirectories)
                    .Select(file => Path.GetRelativePath(cache.LangProject.LinkedFilesRootDir, file))
                    .ToDictionary(file => MediaUriFromPath(file, cache).FileId, file => file);
            }) ?? throw new Exception("Failed to get paths");
    }

    //path is expected to be relative to the LinkedFilesRootDir
    public MediaUri MediaUriFromPath(string path, LcmCache cache)
    {
        if (!File.Exists(Path.Combine(cache.LangProject.LinkedFilesRootDir, path))) return MediaUri.NotFound;
        return new MediaUri(NewGuidV5(path), LocalMediaAuthority);
    }

    public string PathFromMediaUri(MediaUri mediaUri, LcmCache cache)
    {
        var paths = Paths(cache);
        if (mediaUri.Authority != LocalMediaAuthority) throw new ArgumentException("MediaUri must be local", nameof(mediaUri));
        if (paths.TryGetValue(mediaUri.FileId, out var path))
        {
            return path;
        }

        throw new NotFoundException("Media not found: " + mediaUri.FileId, "MedaiUri");
    }

    // produces the same Guid for the same input name
    internal static Guid NewGuidV5(string name)
    {
        return Uuid.NewNameBased(LocalMediaNamespace, name);
    }
}
