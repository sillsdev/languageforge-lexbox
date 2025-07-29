namespace MiniLcm.Models;

public enum MorphType
{
    Unknown,
    BoundRoot,
    BoundStem,
    Circumfix,
    Clitic,
    Enclitic,
    Infix,
    Particle,
    Prefix,
    Proclitic,
    Root,
    Simulfix,
    Stem,
    Suffix,
    Suprafix,
    InfixingInterfix,
    PrefixingInterfix,
    SuffixingInterfix,
    Phrase,
    DiscontiguousPhrase,
    Other,
}

public class MorphTypeData : IObjectWithId<MorphTypeData>
{
    public Guid Id { get; set; }
    public MorphType MorphType { get; set; }
    public MultiString Name { get; set; } = [];
    public MultiString Abbreviation { get; set; } = [];
    public RichMultiString Description { get; set; } = [];
    public string LeadingToken { get; set; } = "";
    public string TrailingToken { get; set; } = "";
    public int SecondaryOrder { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public MorphTypeData Copy()
    {
        return new MorphTypeData
        {
            Id = Id,
            MorphType = MorphType,
            Name = Name.Copy(),
            Abbreviation = Abbreviation.Copy(),
            Description = Description.Copy(),
            LeadingToken = LeadingToken,
            TrailingToken = TrailingToken,
            SecondaryOrder = SecondaryOrder,
            DeletedAt = DeletedAt,
        };
    }
}
