using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Entities;

namespace LcmCrdt.Objects;

public record CrdtComplexFormComponent : ComplexFormComponent, IObjectBase<CrdtComplexFormComponent>
{
    Guid IObjectBase.Id
    {
        get => Id;
        init => Id = value;
    }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid[] GetReferences()
    {
        Span<Guid> senseId = (ComponentSenseId.HasValue ? [ComponentSenseId.Value] : []);
        return [
                ComplexFormEntryId,
                ComponentEntryId,
                ..senseId
            ];
    }

    public void RemoveReference(Guid id, Commit commit)
    {
        if (ComponentEntryId == id || ComplexFormEntryId == id || ComponentSenseId == id)
            DeletedAt = commit.DateTime;
    }

    public IObjectBase Copy()
    {
        return new CrdtComplexFormComponent
        {
            Id = Id,
            ComplexFormEntryId = ComplexFormEntryId,
            ComplexFormHeadword = ComplexFormHeadword,
            ComponentEntryId = ComponentEntryId,
            ComponentHeadword = ComponentHeadword,
            ComponentSenseId = ComponentSenseId,
            DeletedAt = DeletedAt,
        };
    }
}
