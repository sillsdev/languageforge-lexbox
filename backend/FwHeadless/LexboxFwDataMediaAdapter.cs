using FwDataMiniLcmBridge.Media;
using FwHeadless.Media;
using LexCore.Entities;
using LexCore.Exceptions;
using Microsoft.Extensions.Options;
using MiniLcm;
using MiniLcm.Media;
using SIL.LCModel;
using MediaFile = LexCore.Entities.MediaFile;

namespace FwHeadless;

//will be used to lookup ids from lexbox db based on the MediaUri
public class LexboxFwDataMediaAdapter(IOptions<FwHeadlessConfig> config, MediaFileService mediaFileService) : IMediaAdapter
{
    public MediaUri MediaUriFromPath(string path, LcmCache cache)
    {
        if (!Path.IsPathRooted(path)) throw new ArgumentException("Path must be absolute, " + path, nameof(path));
        if (!File.Exists(path)) return MediaUri.NotFound;
        return MediaUriForMediaFile(mediaFileService.FindMediaFile(config.Value.LexboxProjectId(cache), path));
    }

    public string? PathFromMediaUri(MediaUri mediaUri, LcmCache cache)
    {
        var mediaFile = mediaFileService.FindMediaFile(mediaUri.FileId);
        if (mediaFile is null) return null;
        var fullFilePath = Path.Join(cache.ProjectId.ProjectFolder, mediaFile.Filename);
        return fullFilePath;
    }

    private MediaUri MediaUriForMediaFile(MediaFile mediaFile)
    {
        return new MediaUri(mediaFile.Id, config.Value.MediaFileAuthority);
    }
}
