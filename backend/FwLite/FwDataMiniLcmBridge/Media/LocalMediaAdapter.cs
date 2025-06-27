using System.Diagnostics.CodeAnalysis;
using MiniLcm;
using MiniLcm.Exceptions;
using SIL.LCModel;
using UUIDNext;

namespace FwDataMiniLcmBridge.Media;

public class LocalMediaAdapter : IMediaAdapter
{
    //probably don't change this
    private static readonly Guid LocalMediaNamespace = new Guid("45e563a3-f5a6-4d7a-9722-8d7d4d3adfa2");
    private const string LocalMediaAuthority = "localhost";
    private Dictionary<Guid, string>? _paths;


    [MemberNotNull(nameof(_paths))]
    private void EnsurePaths(LcmCache cache)
    {
        if (_paths != null) return;
        _paths = [];
        foreach (var enumerateFile in Directory.EnumerateFiles(cache.LangProject.LinkedFilesRootDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(cache.LangProject.LinkedFilesRootDir, enumerateFile);
            var fileId = MediaUriFromPath(relativePath, cache).FileId;
            _paths[fileId] = relativePath;
        }
    }

    //path is expected to be relative to the LinkedFilesRootDir
    public MediaUri MediaUriFromPath(string path, LcmCache cache)
    {
        if (!File.Exists(Path.Combine(cache.LangProject.LinkedFilesRootDir, path))) throw new FileNotFoundException("File not found in LinkedFiles folder", path);
        return new MediaUri(NewGuidV5(path), LocalMediaAuthority);
    }

    public string PathFromMediaUri(MediaUri mediaUri, LcmCache cache)
    {
        EnsurePaths(cache);
        if (mediaUri.Authority != LocalMediaAuthority) throw new ArgumentException("MediaUri must be local", nameof(mediaUri));
        if (_paths.TryGetValue(mediaUri.FileId, out var path))
        {
            return path;
        }

        throw new NotFoundException("Media not found: " + mediaUri.FileId, "MedaiUri");
    }

    // produces the same Guid for the same input name
    private static Guid NewGuidV5(string name)
    {
        return Uuid.NewNameBased(LocalMediaNamespace, name);
    }
}
