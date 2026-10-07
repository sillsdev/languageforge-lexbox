using LcmCrdt.MediaServer;
using Microsoft.Extensions.Logging;
using MiniLcm.Media;

namespace LcmCrdt.MiniLcmImp;

public class CrdtMediaApi(
    LcmMediaService lcmMediaService,
    CurrentProjectService projectService,
    ILogger<CrdtMediaApi> logger)
{
    public async Task<ReadFileResponse> GetFileStream(MediaUri mediaUri, bool downloadIfMissing = true)
    {
        if (mediaUri == MediaUri.NotFound) return new ReadFileResponse(ReadFileResult.NotFound);
        return await lcmMediaService.GetFileStream(mediaUri.FileId, downloadIfMissing);
    }

    public async Task<UploadFileResponse> SaveFile(Stream stream, LcmFileMetadata metadata)
    {
        try
        {
            if (stream.SafeLength() > MediaFile.MaxFileSize) return new UploadFileResponse(UploadFileResult.TooBig);
            var (result, newResource) = await lcmMediaService.SaveFile(stream, metadata);
            var mediaUri = new MediaUri(result.Id, projectService.ProjectData.ServerId ?? "lexbox.org");
            return new UploadFileResponse(mediaUri, savedToLexbox: result.Remote, newResource);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to save file {Filename}", metadata.Filename);
            return new UploadFileResponse(e.Message);
        }
    }
}
