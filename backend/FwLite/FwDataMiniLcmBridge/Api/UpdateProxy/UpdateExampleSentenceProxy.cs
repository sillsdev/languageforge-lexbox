using MiniLcm.Models;
using SIL.Extensions;
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

    public override IList<Translation> Translations
    {
        get
        {
            return new UpdateListProxy<Translation>(newTranslation =>
                {
                    lexboxLcmApi.CreateExampleSentenceTranslation(sentence, newTranslation);
                },
                deleteTranslation =>
                {
                    sentence.TranslationsOC.RemoveAll(t => t.Guid == deleteTranslation.Id);
                },
                i => throw new NotImplementedException("need to create a translation proxy and return it here"),
                sentence.TranslationsOC.Count
            );
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
