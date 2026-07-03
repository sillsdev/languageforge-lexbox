using System.Diagnostics.CodeAnalysis;
using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

/// <summary>
/// Applies patches of a variant link's own fields (HideMinorEntry, Comment) to the
/// LexEntryRef. Endpoints and Types are not patchable — links are recreated for endpoint
/// changes and types go through Add/RemoveVariantType.
/// </summary>
public record UpdateVariantProxy : Variant
{
    private readonly ILexEntryRef _lexEntryRef;
    private readonly FwDataMiniLcmApi _lexboxLcmApi;

    [SetsRequiredMembers]
    public UpdateVariantProxy(ILexEntryRef lexEntryRef, FwDataMiniLcmApi lexboxLcmApi)
    {
        _lexEntryRef = lexEntryRef;
        _lexboxLcmApi = lexboxLcmApi;
    }

    public override required Guid VariantEntryId
    {
        get => _lexEntryRef.Owner.Guid;
        set => throw new NotImplementedException();
    }

    public override required Guid MainEntryId
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override Guid? MainSenseId
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override List<VariantType> Types
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override bool HideMinorEntry
    {
        get => _lexEntryRef.HideMinorEntry != 0;
        set => _lexEntryRef.HideMinorEntry = value ? 1 : 0;
    }

    public override RichMultiString Comment
    {
        get => new UpdateRichMultiStringProxy(_lexEntryRef.Summary, _lexboxLcmApi);
        set
        {
        }
    }
}
