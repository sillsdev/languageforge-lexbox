using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Entities;

namespace LcmCrdt.Objects;

public class CrdtComplexFormType : ComplexFormType, IObjectBase<CrdtComplexFormType>
{
    Guid IObjectBase.Id
    {
        get => Id;
        init => Id = value;
    }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, Commit commit)
    {
    }

    public IObjectBase Copy()
    {
        return new CrdtComplexFormType { Id = Id, Name = Name, DeletedAt = DeletedAt, };
    }
}
