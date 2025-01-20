using System.Text.Json.Serialization;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class AddEntryComponentChange : CreateChange<ComplexFormComponent>, ISelfNamedType<AddEntryComponentChange>
{
    public double Order { get; }
    public Guid ComplexFormEntryId { get; }
    public Guid ComponentEntryId { get; }
    public Guid? ComponentSenseId { get; }

    [JsonConstructor]
    public AddEntryComponentChange(Guid entityId,
        double order,
        Guid complexFormEntryId,
        Guid componentEntryId,
        Guid? componentSenseId = null) : base(entityId)
    {
        Order = order;
        ComplexFormEntryId = complexFormEntryId;
        ComponentEntryId = componentEntryId;
        ComponentSenseId = componentSenseId;
    }

    public AddEntryComponentChange(ComplexFormComponent component) : this(component.Id == default ? Guid.NewGuid() : component.Id,
        component.Order,
        component.ComplexFormEntryId,
        component.ComponentEntryId,
        component.ComponentSenseId)
    {
    }

    public override async ValueTask<ComplexFormComponent> NewEntity(Commit commit, ChangeContext context)
    {
        var complexFormEntry = await context.GetCurrent<Entry>(ComplexFormEntryId);
        var componentEntry = await context.GetCurrent<Entry>(ComponentEntryId);
        Sense? componentSense = null;
        if (ComponentSenseId is not null)
            componentSense = await context.GetCurrent<Sense>(ComponentSenseId.Value);
        return new ComplexFormComponent
        {
            Id = EntityId,
            Order = Order,
            ComplexFormEntryId = ComplexFormEntryId,
            ComplexFormHeadword = complexFormEntry?.Headword(),
            ComponentEntryId = ComponentEntryId,
            ComponentHeadword = componentEntry?.Headword(),
            ComponentSenseId = ComponentSenseId,
            DeletedAt = (complexFormEntry?.DeletedAt is not null ||
                         componentEntry?.DeletedAt is not null ||
                         (ComponentSenseId.HasValue && componentSense?.DeletedAt is not null))
                ? commit.DateTime
                : (DateTime?)null,
        };
    }
}
