using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdateExampleSentenceProxy(ILexExampleSentence sentence, FwDataMiniLcmApi lexboxLcmApi) : ExampleSentence
{
    public override Guid Id
    {
        get => sentence.Guid;
        set => throw new NotImplementedException();
    }

    public override RichMultiString Sentence
    {
        get => new UpdateRichMultiStringProxy(sentence.Example, lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override IList<Translation> Translations
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override RichString? Reference
    {
        get => throw new NotImplementedException();
        set =>
            sentence.Reference = value is null
                ? null
                : RichTextMapping.ToTsString(value, ws => lexboxLcmApi.GetWritingSystemHandle(ws));
    }
}
