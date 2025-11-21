using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdateSemanticDomainProxy : SemanticDomain
{
    private readonly ICmSemanticDomain _lcmSemanticDomain;
    private readonly FwDataMiniLcmApi _lexboxLcmApi;

    public UpdateSemanticDomainProxy(ICmSemanticDomain lcmSemanticDomain, FwDataMiniLcmApi lexboxLcmApi)
    {
        _lcmSemanticDomain = lcmSemanticDomain;
        Id = lcmSemanticDomain.Guid;
        _lexboxLcmApi = lexboxLcmApi;
    }

    public override MultiString Name
    {
        get => new UpdateMultiStringProxy(_lcmSemanticDomain.Name, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override string Code
    {
        get => _lcmSemanticDomain.Abbreviation.BestAnalysisVernacularAlternative.Text;
        set => throw new NotImplementedException();
    }
}
