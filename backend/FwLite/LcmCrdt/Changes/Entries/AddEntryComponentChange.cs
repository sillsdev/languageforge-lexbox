using System.Text.Json.Serialization;
using LcmCrdt.Objects;
using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class AddEntryComponentChange : CreateChange<CrdtComplexFormComponent>, ISelfNamedType<AddEntryComponentChange>
{
    public Guid ComplexFormEntryId { get; }
    public string? ComplexFormHeadword { get; }
    public Guid ComponentEntryId { get; }
    public Guid? ComponentSenseId { get; }
    public string? ComponentHeadword { get; }

    [JsonConstructor]
    public AddEntryComponentChange(Guid entityId,
        Guid complexFormEntryId,
        string? complexFormHeadword,
        Guid componentEntryId,
        string? componentHeadword,
        Guid? componentSenseId = null) : base(entityId)
    {
        ComplexFormEntryId = complexFormEntryId;
        ComplexFormHeadword = complexFormHeadword;
        ComponentEntryId = componentEntryId;
        ComponentHeadword = componentHeadword;
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

    public override async ValueTask<IObjectBase> NewEntity(Commit commit, ChangeContext context)
    {
        return new CrdtComplexFormComponent
        {
            Id = EntityId,
            ComplexFormEntryId = ComplexFormEntryId,
            ComplexFormHeadword = ComplexFormHeadword,
            ComponentEntryId = ComponentEntryId,
            ComponentHeadword = ComponentHeadword,
            ComponentSenseId = ComponentSenseId,
            DeletedAt = (await context.IsObjectDeleted(ComponentEntryId) ||
                         await context.IsObjectDeleted(ComplexFormEntryId) ||
                         ComponentSenseId.HasValue && await context.IsObjectDeleted(ComponentSenseId.Value))
                ? commit.DateTime
                : (DateTime?)null,
        };
    }
}
