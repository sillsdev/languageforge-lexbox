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

    //the frontend may sometimes try to issue patches to remove Domain.Code or Name, but all we care about is Id
    //when those cases happen then Id will be default, so we ignore them.
    public new IList<UpdateProxySemanticDomain> SemanticDomains
    {
        get => new UpdateListProxy<UpdateProxySemanticDomain>(
            semanticDomain =>
            {
                if (semanticDomain.Id != default) sense.SemanticDomainsRC.Add(lexboxLcmApi.GetLcmSemanticDomain(semanticDomain.Id));
            },
            semanticDomain =>
            {
                if (semanticDomain.Id != default) sense.SemanticDomainsRC.Remove(sense.SemanticDomainsRC.First(sd => sd.Guid == semanticDomain.Id));
            },
            i => new UpdateProxySemanticDomain { Id = sense.SemanticDomainsRC.ElementAt(i).Guid },
            sense.SemanticDomainsRC.Count
        );
        set => throw new NotImplementedException();
    }

    public class UpdateProxySemanticDomain
    {
        public Guid Id { get; set; }
        public string? Code { get; set; }
        public MultiString? Name { get; set; }
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
