using System.Text.Json.Serialization;
using LcmCrdt.Objects;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class AddEntryComponentChange : CreateChange<CrdtComplexFormComponent>, ISelfNamedType<AddEntryComponentChange>
{
    public Guid ComplexFormEntryId { get; }
    public string ComplexFormHeadword { get; }
    public Guid ComponentEntryId { get; }
    public Guid? ComponentSenseId { get; }
    public string ComponentHeadword { get; }

    [JsonConstructor]
    protected AddEntryComponentChange(Guid entityId,
        Guid complexFormEntryId,
        string complexFormHeadword,
        Guid componentEntryId,
        string componentHeadword,
        Guid? componentSenseId = null) : base(entityId)
    {
        ComplexFormEntryId = complexFormEntryId;
        ComplexFormHeadword = complexFormHeadword;
        ComponentEntryId = componentEntryId;
        ComponentHeadword = componentHeadword;
        ComponentSenseId = componentSenseId;
    }

    public AddEntryComponentChange(
        MiniLcm.Models.Entry complexEntry,
        MiniLcm.Models.Entry componentEntry,
        Guid? componentSenseId = null) : this(Guid.NewGuid(),
        complexEntry.Id,
        complexEntry.Headword(),
        componentEntry.Id,
        componentEntry.Headword(),
        componentSenseId)
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
