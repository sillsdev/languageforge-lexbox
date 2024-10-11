using System.Text.Json.Serialization;
using LcmCrdt.Objects;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class SetComplexFormComponentChange : EditChange<CrdtComplexFormComponent>, ISelfNamedType<SetComplexFormComponentChange>
{
    [JsonConstructor]
    protected SetComplexFormComponentChange(Guid entityId, Guid? complexFormEntryId, Guid? componentEntryId, Guid? componentSenseId) : base(entityId)
    {
        ComplexFormEntryId = complexFormEntryId;
        ComponentEntryId = componentEntryId;
        ComponentSenseId = componentSenseId;
    }

    public static SetComplexFormComponentChange NewComplexForm(Guid id, Guid complexFormEntryId) => new(id, complexFormEntryId, null, null);
    public static SetComplexFormComponentChange NewComponent(Guid id, Guid componentEntryId) => new(id, null, componentEntryId, null);
    public static SetComplexFormComponentChange NewComponentSense(Guid id, Guid componentEntryId, Guid? componentSenseId) => new(id, null, componentEntryId, componentSenseId);
    public Guid? ComplexFormEntryId { get; }
    public Guid? ComponentEntryId { get; }
    public Guid? ComponentSenseId { get; }
    public override async ValueTask ApplyChange(CrdtComplexFormComponent entity, ChangeContext context)
    {
        if (ComplexFormEntryId.HasValue)
        {
            entity.ComplexFormEntryId = ComplexFormEntryId.Value;
            var complexFormEntry = (await context.GetSnapshot(ComplexFormEntryId.Value))?.Entity.Is<Entry>();
            entity.ComplexFormHeadword = complexFormEntry?.Headword();
            entity.DeletedAt = complexFormEntry?.DeletedAt != null ? context.Commit.DateTime : (DateTime?)null;
        }
        if (ComponentEntryId.HasValue)
        {
            entity.ComponentEntryId = ComponentEntryId.Value;
            var componentEntry = (await context.GetSnapshot(ComponentEntryId.Value))?.Entity.Is<Entry>();
            entity.ComponentHeadword = componentEntry?.Headword();
            entity.DeletedAt = componentEntry?.DeletedAt != null ? context.Commit.DateTime : (DateTime?)null;
        }
        entity.ComponentSenseId = ComponentSenseId;
        if (ComponentSenseId.HasValue)
        {
            entity.DeletedAt = await context.IsObjectDeleted(ComponentSenseId.Value) ? context.Commit.DateTime : (DateTime?)null;
        }
    }
}
