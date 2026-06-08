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
            DefaultedAt = DefaultedAt,
            Name = Name.Copy()
        };
    }
    public DateTimeOffset? DefaultedAt { get; set; }
    public virtual MultiString Name { get; set; } = new();
}
