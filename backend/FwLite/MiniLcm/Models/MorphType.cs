using System.Text.Json.Serialization;

namespace MiniLcm.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
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
    public virtual Guid Id { get; set; }
    public virtual MorphType MorphType { get; set; }
    public virtual MultiString Name { get; set; } = [];
    public virtual MultiString Abbreviation { get; set; } = [];
    public virtual RichMultiString Description { get; set; } = [];
    public virtual string LeadingToken { get; set; } = "";
    public virtual string TrailingToken { get; set; } = "";
    public virtual int SecondaryOrder { get; set; }

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
