using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using LcmCrdt.Utils;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateMorphTypeChange : CreateChange<MorphType>, ISelfNamedType<CreateMorphTypeChange>
{
    [SetsRequiredMembers]
    public CreateMorphTypeChange(MorphType morphType) : base(morphType.Id)
    {
        if (!CanonicalMorphTypes.IsCanonical(morphType.Id))
        {
            throw new ArgumentException(
                $"Morph type Id {morphType.Id} is not a canonical morph-type Id. Custom morph types are not supported.",
                nameof(morphType));
        }
        Name = morphType.Name;
        Abbreviation = morphType.Abbreviation;
        Description = morphType.Description;
        Prefix = morphType.Prefix;
        Postfix = morphType.Postfix;
        SecondaryOrder = morphType.SecondaryOrder;
        Kind = morphType.Kind;
    }

    [JsonConstructor]
    private CreateMorphTypeChange(Guid entityId) : base(entityId)
    {
    }

    public required MultiString Name { get; init; }
    public required MultiString Abbreviation { get; init; }
    public required RichMultiString Description { get; init; }
    public string? Prefix { get; set; }
    public string? Postfix { get; set; }
    public int SecondaryOrder { get; set; }
    public required MorphTypeKind Kind { get; init; }
    public override async ValueTask<MorphType> NewEntity(Commit commit, IChangeContext context)
    {
        var alreadyExists = await context.GetObjectsOfType<MorphType>().AnyAsync(m => m.Kind == Kind);
        // Can't return null for duplicates or we'll break the API, but returning a pre-deleted object will
        // ensure the duplicate never reaches the DB as our code will filter it out before saving
        return new MorphType
        {
            Id = EntityId,
            Name = Name,
            Abbreviation = Abbreviation,
            Description = Description,
            Prefix = Prefix,
            Postfix = Postfix,
            SecondaryOrder = SecondaryOrder,
            Kind = Kind,
            DeletedAt = alreadyExists ? commit.DateTime : null
        };
    }
}
