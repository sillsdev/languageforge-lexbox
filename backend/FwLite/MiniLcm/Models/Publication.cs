namespace MiniLcm.Models;

public class Publication : IPossibility
{
    public required Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
        return;
    }

    public IObjectWithId Copy()
    {
        return new Publication()
        {
            Id = Id,
            DeletedAt = DeletedAt,
            Name = Name.Copy()
        };
    }
    public virtual MultiString Name { get; set; } = new();

    // used as a marker for whether this type supports crdts, once it does remove this property and fix any code related to it
    public static bool SupportsCrdts => false;
}
