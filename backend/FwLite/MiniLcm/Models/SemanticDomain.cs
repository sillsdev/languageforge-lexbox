namespace MiniLcm.Models;

public class SemanticDomain : IObjectWithId
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
