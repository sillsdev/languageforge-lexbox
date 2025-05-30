using MiniLcm.Models;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

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

    public override RichMultiString Translation
    {
        get
        {
            var firstTranslation = sentence.TranslationsOC.FirstOrDefault()?.Translation;
            if (firstTranslation is null)
            {
                var translation = lexboxLcmApi.CreateExampleSentenceTranslation(sentence);
                sentence.TranslationsOC.Add(translation);
                firstTranslation = translation.Translation;
            }
            return new UpdateRichMultiStringProxy(firstTranslation, lexboxLcmApi);
        }
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
