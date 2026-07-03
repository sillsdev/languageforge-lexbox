using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class AddVariantTypeChange(Guid entityId, VariantType variantType)
    : EditChange<Variant>(entityId), ISelfNamedType<AddVariantTypeChange>
{
    public VariantType VariantType { get; } = variantType;

    public override async ValueTask ApplyChange(Variant entity, IChangeContext context)
    {
        if (entity.Types.Any(t => t.Id == VariantType.Id)) return;
        if (await context.IsObjectDeleted(VariantType.Id)) return;
        entity.Types.Add(VariantType);
    }
}
