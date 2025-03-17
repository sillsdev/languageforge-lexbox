using System.Diagnostics.CodeAnalysis;
using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public record UpdateComplexFormTypeProxy : ComplexFormType
{
    private readonly ILexEntryType _lexEntryType;
    private readonly FwDataMiniLcmApi _lexboxLcmApi;

    [SetsRequiredMembers]
    public UpdateComplexFormTypeProxy(ILexEntryType lexEntryType, FwDataMiniLcmApi lexboxLcmApi)
    {
        _lexEntryType = lexEntryType;
        _lexboxLcmApi = lexboxLcmApi;
        Name = base.Name = new();
    }

    public override Guid Id
    {
        get => _lexEntryType.Guid;
        set => throw new NotImplementedException();
    }

    public override required MultiString Name
    {
        get => new UpdateMultiStringProxy(_lexEntryType.Name, _lexboxLcmApi);
        set
        {
        }
    }
}
