namespace MiniLcm.Models;

public class PartOfSpeech : IObjectWithId
{
    public Guid Id { get; set; }
    public virtual MultiString Name { get; set; } = new();
    // TODO: Probably need Abbreviation in order to match LCM data model

    public DateTimeOffset? DeletedAt { get; set; }
    public string? Version { get; set; }
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
        return new PartOfSpeech
        {
            Id = Id,
            Name = Name,
            DeletedAt = DeletedAt,
            Version = Version,
            Predefined = Predefined
        };
    }
}
