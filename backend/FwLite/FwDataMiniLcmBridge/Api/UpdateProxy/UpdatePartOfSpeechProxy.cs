using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdatePartOfSpeechProxy : PartOfSpeech
{
    private readonly IPartOfSpeech _lcmPartOfSpeech;
    private readonly FwDataMiniLcmApi _lexboxLcmApi;

    public UpdatePartOfSpeechProxy(IPartOfSpeech lcmPartOfSpeech, FwDataMiniLcmApi lexboxLcmApi)
    {
        _lcmPartOfSpeech = lcmPartOfSpeech;
        Id = lcmPartOfSpeech.Guid;
        _lexboxLcmApi = lexboxLcmApi;
    }

    public override MultiString Name
    {
        get => new UpdateMultiStringProxy(_lcmPartOfSpeech.Name, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override MultiString Abbreviation
    {
        get => new UpdateMultiStringProxy(_lcmPartOfSpeech.Abbreviation, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override RichMultiString Description
    {
        get => new UpdateRichMultiStringProxy(_lcmPartOfSpeech.Description, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }
}
