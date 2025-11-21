using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdateTranslationProxy(ICmTranslation translation, FwDataMiniLcmApi api): Translation
{
    public override RichMultiString Text
    {
        get => new UpdateRichMultiStringProxy(translation.Translation, api);
        set => throw new NotImplementedException();
    }
}
