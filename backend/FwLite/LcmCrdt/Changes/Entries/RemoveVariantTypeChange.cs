using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class RemoveVariantTypeChange(Guid entityId, Guid variantTypeId) : EditChange<Variant>(entityId), ISelfNamedType<RemoveVariantTypeChange>
{
    public Guid VariantTypeId { get; } = variantTypeId;
    public override ValueTask ApplyChange(Variant entity, IChangeContext context)
    {
        entity.Types.RemoveAll(t => t.Id == VariantTypeId);
        return ValueTask.CompletedTask;
    }
}
