using LcmCrdt.Utils;
using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateMorphTypeDataChange(Guid entityId, MultiString name, MultiString abbreviation, RichMultiString description, string? leadingToken, string? trailingToken, int secondaryOrder, MorphType morphType) : CreateChange<MorphTypeData>(entityId), ISelfNamedType<CreateMorphTypeDataChange>
{
    public MultiString Name { get; } = name;
    public MultiString Abbreviation { get; set; } = abbreviation;
    public RichMultiString Description { get; set; } = description;
    public string? LeadingToken { get; set; } = leadingToken;
    public string? TrailingToken { get; set; } = trailingToken;
    public int SecondaryOrder { get; set; } = secondaryOrder;
    public MorphType MorphType { get; set; } = morphType;
    public override async ValueTask<MorphTypeData> NewEntity(Commit commit, IChangeContext context)
    {
        var alreadyExists = await context.GetObjectsOfType<MorphTypeData>().AnyAsync(m => m.MorphType == MorphType);
        // Can't return null for duplicates or we'll break the API, but the DB will auto-discard the duplicate
        // However, we should still set DeletedAt on the duplicate so that mocks can still ensure correct behavior
        return new MorphTypeData
        {
            Id = EntityId,
            Name = Name,
            Abbreviation = Abbreviation,
            Description = Description,
            LeadingToken = LeadingToken,
            TrailingToken = TrailingToken,
            SecondaryOrder = SecondaryOrder,
            MorphType = MorphType,
            DeletedAt = alreadyExists ? commit.DateTime : null
        };
    }
}
