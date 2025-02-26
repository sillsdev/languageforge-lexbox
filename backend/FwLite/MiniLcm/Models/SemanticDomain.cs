namespace MiniLcm.Models;

public class SemanticDomain : IObjectWithId<SemanticDomain>
{
    public virtual Guid Id { get; set; }
    public virtual MultiString Name { get; set; } = new();
    public virtual string Code { get; set; } = string.Empty;
    public DateTimeOffset? DeletedAt { get; set; }
    public bool Predefined { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public SemanticDomain Copy()
    {
        return new SemanticDomain
        {
            Id = Id,
            Code = Code,
            Name = Name.Copy(),
            DeletedAt = DeletedAt,
            Predefined = Predefined
        };
    }
}
