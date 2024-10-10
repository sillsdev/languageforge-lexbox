using System.Diagnostics.CodeAnalysis;
using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdateComplexFormTypeProxy : ComplexFormType
{
    private readonly ILexEntryType _lexEntryType;
    private readonly ILexEntry _lcmEntry;
    private readonly FwDataMiniLcmApi _lexboxLcmApi;

    [SetsRequiredMembers]
    public UpdateComplexFormTypeProxy(ILexEntryType lexEntryType, ILexEntry lcmEntry, FwDataMiniLcmApi lexboxLcmApi)
    {
        _lexEntryType = lexEntryType;
        _lcmEntry = lcmEntry;
        _lexboxLcmApi = lexboxLcmApi;
        Name = new();
    }

    public override Guid Id
    {
        get => _lexEntryType.Guid;
        set
        {
            _lexboxLcmApi.RemoveComplexFormType(_lcmEntry, _lexEntryType.Guid);
            _lexboxLcmApi.AddComplexFormType(_lcmEntry, value);
        }
    }
}
