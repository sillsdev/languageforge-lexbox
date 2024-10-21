namespace MiniLcm.Models;

public class PartOfSpeech : IObjectWithId
{
    public Guid Id { get; set; }
    public MultiString Name { get; set; } = new();

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
        return new PartOfSpeech { Id = Id, Name = Name, DeletedAt = DeletedAt, Predefined = Predefined };
    }
}
