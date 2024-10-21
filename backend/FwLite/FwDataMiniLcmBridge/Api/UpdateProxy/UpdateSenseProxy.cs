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
        set { }
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
                sense.MorphoSyntaxAnalysisRA.SetMsaPartOfSpeech(null);
            }
        }
    }

    //the frontend may sometimes try to issue patches to remove Domain.Code or Name, but all we care about is Id
    //when those cases happen then Id will be default, so we ignore them.
    public override IList<SemanticDomain> SemanticDomains
    {
        get =>
            new UpdateListProxy<SemanticDomain>(
                semanticDomain =>
                {
#pragma warning disable VSTHRD002
                    if (semanticDomain.Id != default && lexboxLcmApi.GetLcmSemanticDomain(semanticDomain.Id) is { } lcmSemanticDomain)
                        sense.SemanticDomainsRC.Add(lcmSemanticDomain);
                },
                semanticDomain =>
                {
                    lexboxLcmApi.RemoveSemanticDomainFromSense(sense.Guid, semanticDomain.Id).Wait();
#pragma warning restore VSTHRD002
                },
                i => new UpdateProxySemanticDomain(sense.SemanticDomainsRC,
                    sense.SemanticDomainsRC.ElementAt(i).Guid,
                    lexboxLcmApi),
                sense.SemanticDomainsRC.Count
            );
        set => throw new NotImplementedException();
    }

    public class UpdateProxySemanticDomain : SemanticDomain
    {
        private readonly ILcmReferenceCollection<ICmSemanticDomain> _senseSemanticDomainsRc;
        private Guid _id;
        private readonly FwDataMiniLcmApi _lexboxLcmApi;

        [SetsRequiredMembers]
        public UpdateProxySemanticDomain(ILcmReferenceCollection<ICmSemanticDomain> senseSemanticDomainsRc,
            Guid id,
            FwDataMiniLcmApi lexboxLcmApi)
        {
            _senseSemanticDomainsRc = senseSemanticDomainsRc;
            _id = id;
            _lexboxLcmApi = lexboxLcmApi;
            Name = new MultiString();
            Code = "";
        }

        public override required Guid Id
        {
            get => _id;
            set
            {
                if (value == _id) return;
                if (value == default) throw new ArgumentException("Cannot set to default");
                _senseSemanticDomainsRc.Remove(_senseSemanticDomainsRc.First(sd => sd.Guid == _id));
                var lcmSemanticDomain = _lexboxLcmApi.GetLcmSemanticDomain(value);
                if (lcmSemanticDomain is not null) _senseSemanticDomainsRc.Add(lcmSemanticDomain);
                _id = value;
            }
        }
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
