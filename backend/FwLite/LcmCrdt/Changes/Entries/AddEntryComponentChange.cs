using System.Text.Json.Serialization;
using LcmCrdt.Objects;
using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class AddEntryComponentChange : CreateChange<ComplexFormComponent>, ISelfNamedType<AddEntryComponentChange>
{
    public Guid ComplexFormEntryId { get; }
    public Guid ComponentEntryId { get; }
    public Guid? ComponentSenseId { get; }

    [JsonConstructor]
    public AddEntryComponentChange(Guid entityId,
        Guid complexFormEntryId,
        string? complexFormHeadword,
        Guid componentEntryId,
        string? componentHeadword,
        Guid? componentSenseId = null) : base(entityId)
    {
        ComplexFormEntryId = complexFormEntryId;
        ComponentEntryId = componentEntryId;
        ComponentSenseId = componentSenseId;
    }

    public AddEntryComponentChange(ComplexFormComponent component) : this(component.Id == default ? Guid.NewGuid() : component.Id,
        component.ComplexFormEntryId,
        component.ComplexFormHeadword,
        component.ComponentEntryId,
        component.ComponentHeadword,
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
