namespace MiniLcm.Models;

public class Entry : IObjectWithId
{
    public virtual Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public virtual MultiString LexemeForm { get; set; } = new();

    public virtual MultiString CitationForm { get; set; } = new();

    public virtual MultiString LiteralMeaning { get; set; } = new();
    public virtual IList<Sense> Senses { get; set; } = [];

    public virtual MultiString Note { get; set; } = new();

    public bool MatchesQuery(string query) =>
        LexemeForm.SearchValue(query)
        || CitationForm.SearchValue(query)
        || LiteralMeaning.SearchValue(query);

    public string Headword()
    {
        var word = CitationForm.Values.Values.FirstOrDefault();
        if (string.IsNullOrEmpty(word)) word = LexemeForm.Values.Values.FirstOrDefault();
        return word?.Trim() ?? "(Unknown)";
    }


    public IObjectWithId Copy()
    {
        return new Entry
        {
            Id = Id,
            DeletedAt = DeletedAt,
            LexemeForm = LexemeForm.Copy(),
            CitationForm = CitationForm.Copy(),
            LiteralMeaning = LiteralMeaning.Copy(),
            Note = Note.Copy(),
            Senses = [..Senses.Select(s => (Sense)s.Copy())]
        };
    }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }
}
