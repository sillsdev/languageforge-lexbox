using System.Text.Json.Serialization;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class AddVariantTypeChange : EditChange<Variant>, ISelfNamedType<AddVariantTypeChange>
{
    public AddVariantTypeChange(Guid entityId, VariantTypeRef variantTypeRef, BetweenPosition? between = null)
        : base(entityId)
    {
        VariantTypeId = variantTypeRef.Id;
        Order = variantTypeRef.Order;
        Between = between;
    }

    [JsonConstructor]
    private AddVariantTypeChange(Guid entityId) : base(entityId)
    {
    }

    public Guid VariantTypeId { get; set; }
    public double Order { get; set; }
    public BetweenPosition? Between { get; set; }

    public override async ValueTask ApplyChange(Variant entity, IChangeContext context)
    {
        // Skip adding if this is a duplicate change
        if (entity.Types.Any(t => t.Id == VariantTypeId)) return;
        if (await context.IsObjectDeleted(VariantTypeId)) return;
        // Order is resolved from Between against live state at apply time (the ctor/serialized Order is not
        // authoritative), so concurrent adds converge. ReorderVariantTypeChange deliberately differs — it
        // stores a fixed Order — because a reorder targets a specific destination, an add just needs a slot.
        Order = OrderPicker.PickOrder(entity.Types, Between);
        entity.Types.Add(new VariantTypeRef { Id = VariantTypeId, Order = Order });
        entity.Types.Sort(VariantTypeRef.CompareRefs);
    }
}
