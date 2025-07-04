using FwDataMiniLcmBridge.Media;
using FwHeadless.Media;
using LexCore.Entities;
using Microsoft.Extensions.Options;
using MiniLcm;
using SIL.LCModel;

namespace FwHeadless;

//will be used to lookup ids from lexbox db based on the MediaUri
public class LexboxFwDataMediaAdapter(IOptions<FwHeadlessConfig> config, MediaFileService mediaFileService) : IMediaAdapter
{
    public MediaUri MediaUriFromPath(string path, LcmCache cache)
    {
        var fullPath = Path.Join(cache.LangProject.LinkedFilesRootDir, path);
        if (!File.Exists(fullPath)) return MediaUri.NotFound;
        return MediaUriForMediaFile(mediaFileService.FindMediaFile(config.Value.LexboxProjectId(cache), fullPath));
    }

    public string PathFromMediaUri(MediaUri mediaUri, LcmCache cache)
    {
        var mediaFile = mediaFileService.FindMediaFile(mediaUri.FileId);
        var fullFilePath = Path.Join(cache.ProjectId.ProjectFolder, mediaFile.Filename);
        return Path.GetRelativePath(cache.LangProject.LinkedFilesRootDir, fullFilePath);
    }

    private MediaUri MediaUriForMediaFile(MediaFile mediaFile)
    {
        return new MediaUri(mediaFile.Id, config.Value.MediaFileAuthority);
    }
}
