using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class ReorderVariantTypeChange(Guid variantTypeId, Guid entityId, double order)
    : EditChange<Variant>(entityId), ISelfNamedType<ReorderVariantTypeChange>
{
    public Guid VariantTypeId { get; } = variantTypeId;
    public double Order { get; } = order;

    public override ValueTask ApplyChange(Variant entity, IChangeContext context)
    {
        var typeRef = entity.Types.FirstOrDefault(t => t.Id == VariantTypeId);
        // Not found? The type may have been removed by another change, making the reorder a no-op
        if (typeRef is null) return ValueTask.CompletedTask;
        typeRef.Order = Order;
        entity.Types.Sort(VariantTypeRef.CompareRefs);
        return ValueTask.CompletedTask;
    }
}
