using System.Diagnostics.CodeAnalysis;
using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdatePublicationProxy : Publication
{
    private readonly ICmPossibility _lcmPublication;
    private readonly FwDataMiniLcmApi _lexboxLcmApi;

    [SetsRequiredMembers]
    public UpdatePublicationProxy(ICmPossibility lcmPublication, FwDataMiniLcmApi lexboxLcmApi)
    {
        _lcmPublication = lcmPublication;
        Id = lcmPublication.Guid;
        _lexboxLcmApi = lexboxLcmApi;
    }

    public override MultiString Name
    {
        get => new UpdateMultiStringProxy(_lcmPublication.Name, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }
}
