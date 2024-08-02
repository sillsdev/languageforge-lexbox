using SIL.Harmony;
using SIL.Harmony.Entities;

namespace LcmCrdt.Objects;

public class SemanticDomain : MiniLcm.SemanticDomain, IObjectBase<SemanticDomain>
{
    Guid IObjectBase.Id
    {
        get => Id;
        init => Id = value;
    }

    public DateTimeOffset? DeletedAt { get; set; }
    public bool Predefined { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, Commit commit)
    {
    }

    public IObjectBase Copy()
    {
        return new SemanticDomain
        {
            Id = Id,
            Code = Code,
            Name = Name,
            DeletedAt = DeletedAt,
            Predefined = Predefined
        };
    }
}
