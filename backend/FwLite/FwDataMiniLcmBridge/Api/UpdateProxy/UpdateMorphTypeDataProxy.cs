using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdateMorphTypeDataProxy : MorphTypeData
{
    private readonly IMoMorphType _lcmMorphType;
    private readonly FwDataMiniLcmApi _lexboxLcmApi;

    public UpdateMorphTypeDataProxy(IMoMorphType lcmMorphType, FwDataMiniLcmApi lexboxLcmApi)
    {
        _lcmMorphType = lcmMorphType;
        Id = lcmMorphType.Guid;
        _lexboxLcmApi = lexboxLcmApi;
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

    public override string LeadingToken
    {
        get => _lcmMorphType.Prefix;
        set => throw new NotImplementedException();
    }

    public override string TrailingToken
    {
        get => _lcmMorphType.Postfix;
        set => throw new NotImplementedException();
    }

    public override int SecondaryOrder
    {
        get => _lcmMorphType.SecondaryOrder;
        set => throw new NotImplementedException();
    }
}
