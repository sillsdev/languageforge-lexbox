using MiniLcm;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Media;

public interface IMediaAdapter
{
    MediaUri MediaUriFromPath(string path, LcmCache cache);
    string PathFromMediaUri(MediaUri mediaUri, LcmCache cache);
}
