namespace MiniLcm.Models;

public class Publication : IPossibility, IObjectWithId<Publication>
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

    public Publication Copy()
    {
        return new Publication()
        {
            Id = Id,
            DeletedAt = DeletedAt,
            IsMain = IsMain,
            Name = Name.Copy()
        };
    }
    public virtual bool IsMain { get; set; }
    public virtual MultiString Name { get; set; } = new();
}
