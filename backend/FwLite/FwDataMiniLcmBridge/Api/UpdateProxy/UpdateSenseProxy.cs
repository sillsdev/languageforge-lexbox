 using System.Diagnostics.CodeAnalysis;
 using MiniLcm.Models;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdateSenseProxy(ILexSense sense, FwDataMiniLcmApi lexboxLcmApi) : Sense
{
    public override Guid Id
    {
        get => sense.Guid;
        set => throw new NotImplementedException();
    }

    public override RichMultiString Definition
    {
        get => new UpdateRichMultiStringProxy(sense.Definition, lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override MultiString Gloss
    {
        get => new UpdateMultiStringProxy(sense.Gloss, lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override PartOfSpeech? PartOfSpeech
    {
        get => throw new NotImplementedException();
        set { }
    }

    public override Guid? PartOfSpeechId
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override IList<SemanticDomain> SemanticDomains
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override List<ExampleSentence> ExampleSentences
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
}
