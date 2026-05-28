using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdatePictureProxy(ICmPicture picture, FwDataMiniLcmApi lexboxLcmApi) : Picture
{
    public override Guid Id
    {
        get => picture.Guid;
        set => throw new NotImplementedException();
    }

    public override RichMultiString Caption
    {
        get => new UpdateRichMultiStringProxy(picture.Caption, lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    // TODO: MediaUri
}
