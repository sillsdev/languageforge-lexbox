namespace MiniLcm.Models;

public class Sense : IObjectWithId
{
    public virtual Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid EntryId { get; set; }
    public virtual MultiString Definition { get; set; } = new();
    public virtual MultiString Gloss { get; set; } = new();
    public virtual string PartOfSpeech { get; set; } = string.Empty;
    public virtual Guid? PartOfSpeechId { get; set; }
    public virtual IList<SemanticDomain> SemanticDomains { get; set; } = [];
    public virtual IList<ExampleSentence> ExampleSentences { get; set; } = [];

    public Guid[] GetReferences()
    {
        ReadOnlySpan<Guid> pos = PartOfSpeechId.HasValue ? [PartOfSpeechId.Value] : [];
        return [EntryId, ..pos, ..SemanticDomains.Select(sd => sd.Id)];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
        if (id == EntryId)
            DeletedAt = time;
        if (id == PartOfSpeechId)
            PartOfSpeechId = null;
        SemanticDomains = [..SemanticDomains.Where(sd => sd.Id != id)];
    }

    public IObjectWithId Copy()
    {
        return new Sense
        {
            Id = Id,
            EntryId = EntryId,
            DeletedAt = DeletedAt,
            Definition = Definition.Copy(),
            Gloss = Gloss.Copy(),
            PartOfSpeech = PartOfSpeech,
            PartOfSpeechId = PartOfSpeechId,
            SemanticDomains = [..SemanticDomains],
            ExampleSentences = [..ExampleSentences.Select(s => (ExampleSentence)s.Copy())]
        };
    }
}
