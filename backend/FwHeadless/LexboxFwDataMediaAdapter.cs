using FwDataMiniLcmBridge.Media;
using MiniLcm;
using SIL.LCModel;

namespace FwHeadless;

//will be used to lookup ids from lexbox db based on the MediaUri
public class LexboxFwDataMediaAdapter : IMediaAdapter
{
    public MediaUri MediaUriFromPath(string path, LcmCache cache)
    {
        throw new NotImplementedException("Implement once media server upload is implemented");
    }

    public string PathFromMediaUri(MediaUri mediaUri, LcmCache cache)
    {
        throw new NotImplementedException("Implement once media server upload is implemented");
    }
}
