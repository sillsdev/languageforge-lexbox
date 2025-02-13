using System.Diagnostics.CodeAnalysis;
using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public record UpdateComplexFormTypeProxy : ComplexFormType
{
    private readonly ILexEntryType _lexEntryType;
    private readonly ILexEntry? _lcmEntry;
    private readonly FwDataMiniLcmApi _lexboxLcmApi;

    [SetsRequiredMembers]
    public UpdateComplexFormTypeProxy(ILexEntryType lexEntryType, ILexEntry? lcmEntry, FwDataMiniLcmApi lexboxLcmApi)
    {
        _lexEntryType = lexEntryType;
        _lcmEntry = lcmEntry;
        _lexboxLcmApi = lexboxLcmApi;
        Name = base.Name = new();
    }

    public override Guid Id
    {
        get => _lexEntryType.Guid;
        set
        {
            if (_lcmEntry is null)
                throw new InvalidOperationException("Cannot update complex form type Id on a null entry");
            _lexboxLcmApi.RemoveComplexFormType(_lcmEntry, _lexEntryType.Guid);
            _lexboxLcmApi.AddComplexFormType(_lcmEntry, value);
        }
    }

    public override required MultiString Name
    {
        get => new UpdateMultiStringProxy(_lexEntryType.Name, _lexboxLcmApi);
        set
        {
        }
    }
}
