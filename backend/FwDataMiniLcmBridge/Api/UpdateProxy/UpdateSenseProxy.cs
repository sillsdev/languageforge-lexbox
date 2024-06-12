using MiniLcm;
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

    public override MultiString Definition
    {
        get => new UpdateMultiStringProxy(sense.Definition, lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override MultiString Gloss
    {
        get => new UpdateMultiStringProxy(sense.Gloss, lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override string PartOfSpeech
    {
        get => throw new NotImplementedException();
        set {}
    }

    public override Guid? PartOfSpeechId
    {
        get => throw new NotImplementedException();
        set
        {
            if (value.HasValue)
            {
                var partOfSpeech = sense.Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>()
                    .GetObject(value.Value);
                if (sense.MorphoSyntaxAnalysisRA == null)
                {
                    sense.SandboxMSA = SandboxGenericMSA.Create(sense.GetDesiredMsaType(), partOfSpeech);
                }
                else
                {
                    sense.MorphoSyntaxAnalysisRA.SetMsaPartOfSpeech(partOfSpeech);
                }
            }
            else
            {
                sense.MorphoSyntaxAnalysisRA = null;
            }
        }
    }

    public override IList<SemanticDomain> SemanticDomains
    {
        get => new UpdateListProxy<SemanticDomain>(
            semanticDomain => sense.SemanticDomainsRC.Add(lexboxLcmApi.GetLcmSemanticDomain(semanticDomain)),
            semanticDomain => sense.SemanticDomainsRC.Remove(sense.SemanticDomainsRC.First(sd => sd.Guid == semanticDomain.Id)),
            i => new SemanticDomain { Id = sense.SemanticDomainsRC.ElementAt(i).Guid, Code = "", Name = new MultiString() },
            sense.SemanticDomainsRC.Count
        );
        set => throw new NotImplementedException();
    }

    public override IList<ExampleSentence> ExampleSentences
    {
        get =>
            new UpdateListProxy<ExampleSentence>(
                sentence => lexboxLcmApi.CreateExampleSentence(sense, sentence),
                sentence => lexboxLcmApi.DeleteExampleSentence(sense.Owner.Guid, Id, sentence.Id),
                i => new UpdateExampleSentenceProxy(sense.ExamplesOS[i], lexboxLcmApi),
                sense.ExamplesOS.Count
            );
        set => throw new NotImplementedException();
    }
}
