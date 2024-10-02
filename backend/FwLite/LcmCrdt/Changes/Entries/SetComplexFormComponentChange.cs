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
            entity.DeletedAt = await context.IsObjectDeleted(ComplexFormEntryId.Value) ? context.Commit.DateTime : (DateTime?)null;
        }
        if (ComponentEntryId.HasValue)
        {
            entity.ComponentEntryId = ComponentEntryId.Value;
            entity.DeletedAt = await context.IsObjectDeleted(ComponentEntryId.Value) ? context.Commit.DateTime : (DateTime?)null;
        }
        entity.ComponentSenseId = ComponentSenseId;
        if (ComponentSenseId.HasValue)
        {
            entity.DeletedAt = await context.IsObjectDeleted(ComponentSenseId.Value) ? context.Commit.DateTime : (DateTime?)null;
        }
    }
}
