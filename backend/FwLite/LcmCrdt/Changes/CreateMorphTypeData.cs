using LcmCrdt.Objects;
using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateMorphTypeData(Guid entityId, MultiString name, MultiString abbreviation, RichMultiString description, string leadingToken, string trailingToken, int secondaryOrder, MorphType morphType) : CreateChange<MorphTypeData>(entityId), ISelfNamedType<CreateMorphTypeData>
{
    public MultiString Name { get; } = name;
    public MultiString Abbreviation { get; set; } = abbreviation;
    public RichMultiString Description { get; set; } = description;
    public string LeadingToken { get; set; } = leadingToken;
    public string TrailingToken { get; set; } = trailingToken;
    public int SecondaryOrder { get; set; } = secondaryOrder;
    public MorphType MorphType { get; set; } = morphType;
    public override ValueTask<MorphTypeData> NewEntity(Commit commit, IChangeContext context)
    {
        return ValueTask.FromResult(new MorphTypeData
        {
            Id = EntityId,
            Name = Name,
            Abbreviation = Abbreviation,
            Description = Description,
            LeadingToken = LeadingToken,
            TrailingToken = TrailingToken,
            SecondaryOrder = SecondaryOrder,
            MorphType = MorphType,
        });
    }
}
