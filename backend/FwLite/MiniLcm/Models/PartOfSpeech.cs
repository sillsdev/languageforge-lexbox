namespace MiniLcm.Models;

public class PartOfSpeech : IPossibility, IObjectWithId<PartOfSpeech>
{
    public Guid Id { get; set; }
    public virtual MultiString Name { get; set; } = new();
    public virtual MultiString Abbreviation { get; set; } = new();
    public virtual RichMultiString Description { get; set; } = new();

    public DateTimeOffset? DeletedAt { get; set; }
    public bool Predefined { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public PartOfSpeech Copy()
    {
        return new PartOfSpeech
        {
            Id = Id,
            Name = Name.Copy(),
            Abbreviation = Abbreviation.Copy(),
            Description = Description.Copy(),
            DeletedAt = DeletedAt,
            Predefined = Predefined
        };
    }
}
