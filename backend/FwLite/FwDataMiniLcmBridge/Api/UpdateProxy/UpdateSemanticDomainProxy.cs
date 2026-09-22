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

    public override MultiString Abbreviation
    {
        get => new UpdateMultiStringProxy(_lcmSemanticDomain.Abbreviation, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override RichMultiString Description
    {
        get => new UpdateRichMultiStringProxy(_lcmSemanticDomain.Description, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override string Code
    {
        get => LcmHelpers.GetSemanticDomainCode(_lcmSemanticDomain);
        // Derived from Abbreviation on read; ignore writes (do not map Code → Abbreviation).
        set { }
    }

    public override string? OcmCodes
    {
        get => _lcmSemanticDomain.OcmCodes;
        set => _lcmSemanticDomain.OcmCodes = value;
    }

    public override string? LouwNidaCodes
    {
        get => _lcmSemanticDomain.LouwNidaCodes;
        set => _lcmSemanticDomain.LouwNidaCodes = value;
    }
}
