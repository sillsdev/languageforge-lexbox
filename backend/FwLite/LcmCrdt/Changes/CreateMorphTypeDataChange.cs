using System.Text.Json.Serialization;
using LcmCrdt.Utils;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateMorphTypeDataChange : CreateChange<MorphTypeData>, ISelfNamedType<CreateMorphTypeDataChange>
{
    public CreateMorphTypeDataChange(MorphTypeData morphTypeData) : base(morphTypeData.Id == Guid.Empty ? Guid.NewGuid() : morphTypeData.Id)
    {
        morphTypeData.Id = EntityId;
        Name = morphTypeData.Name;
        Name = morphTypeData.Name;
        Description = morphTypeData.Description;
        LeadingToken = morphTypeData.LeadingToken;
        TrailingToken = morphTypeData.TrailingToken;
        SecondaryOrder = morphTypeData.SecondaryOrder;
        MorphType = morphTypeData.MorphType;
    }

    [JsonConstructor]
    private CreateMorphTypeDataChange(Guid entityId) : base(entityId)
    {
    }

    public MultiString? Name { get; set; }
    public MultiString? Abbreviation { get; set; }
    public RichMultiString? Description { get; set; }
    public string? LeadingToken { get; set; }
    public string? TrailingToken { get; set; }
    public int SecondaryOrder { get; set; }
    public MorphType MorphType { get; set; }
    public override async ValueTask<MorphTypeData> NewEntity(Commit commit, IChangeContext context)
    {
        var alreadyExists = await context.GetObjectsOfType<MorphTypeData>().AnyAsync(m => m.MorphType == MorphType);
        // Can't return null for duplicates or we'll break the API, but returning a pre-deleted object will
        // ensure the duplicate never reaches the DB as our code will filter it out before saving
        return new MorphTypeData
        {
            Id = EntityId,
            Name = Name ?? [],
            Abbreviation = Abbreviation ?? [],
            Description = Description ?? [],
            LeadingToken = LeadingToken,
            TrailingToken = TrailingToken,
            SecondaryOrder = SecondaryOrder,
            MorphType = MorphType,
            DeletedAt = alreadyExists ? commit.DateTime : null
        };
    }
}
