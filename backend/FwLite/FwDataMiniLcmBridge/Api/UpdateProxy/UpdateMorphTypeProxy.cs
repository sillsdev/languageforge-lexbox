using System.Diagnostics.CodeAnalysis;
using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdateMorphTypeProxy : MorphType
{
    private readonly IMoMorphType _lcmMorphType;
    private readonly FwDataMiniLcmApi _lexboxLcmApi;

    [SetsRequiredMembers]
    public UpdateMorphTypeProxy(IMoMorphType lcmMorphType, FwDataMiniLcmApi lexboxLcmApi)
    {
        _lcmMorphType = lcmMorphType;
        Id = lcmMorphType.Guid;
        _lexboxLcmApi = lexboxLcmApi;
        Kind = LcmHelpers.FromLcmMorphType(lcmMorphType);
    }

    public override MultiString Name
    {
        get => new UpdateMultiStringProxy(_lcmMorphType.Name, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override MultiString Abbreviation
    {
        get => new UpdateMultiStringProxy(_lcmMorphType.Abbreviation, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override RichMultiString Description
    {
        get => new UpdateRichMultiStringProxy(_lcmMorphType.Description, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override string? Prefix
    {
        get => _lcmMorphType.Prefix;
        set => _lcmMorphType.Prefix = value;
    }

    public override string? Postfix
    {
        get => _lcmMorphType.Postfix;
        set => _lcmMorphType.Postfix = value;
    }

    public override int SecondaryOrder
    {
        get => _lcmMorphType.SecondaryOrder;
        set => _lcmMorphType.SecondaryOrder = value;
    }
}
