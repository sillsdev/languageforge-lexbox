namespace MiniLcm.Models;

public class SemanticDomain : IObjectWithId
{
    public virtual required Guid Id { get; set; }
    public virtual required MultiString Name { get; set; }
    public virtual required string Code { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool Predefined { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public IObjectWithId Copy()
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
