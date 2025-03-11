using MiniLcm.Models;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdateExampleSentenceProxy(ILexExampleSentence sentence, FwDataMiniLcmApi lexboxLcmApi): ExampleSentence
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

    public override RichMultiString Translation
    {
        get
        {
            var firstTranslation = sentence.TranslationsOC.FirstOrDefault()?.Translation;
            return firstTranslation is null ? new RichMultiString() : new UpdateRichMultiStringProxy(firstTranslation, lexboxLcmApi);
        }
        set => throw new NotImplementedException();
    }

    public override string? Reference
    {
        get => throw new NotImplementedException();
        set => sentence.Reference = TsStringUtils.MakeString(value, sentence.Reference.get_WritingSystem(0));
    }
}
