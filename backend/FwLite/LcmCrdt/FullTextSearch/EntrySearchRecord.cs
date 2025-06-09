using LinqToDB.Mapping;

namespace LcmCrdt.FullTextSearch;

public record EntrySearchRecord : FullTextSearchRecord
{
    public Guid Id { get; init; }
    public required string Headword { get; init; }
    public required string CitationForm { get; init; }
    public required string LexemeForm { get; init; }
    public required string Gloss { get; init; }
    public required string Definition { get; init; }
}

public record FullTextSearchRecord
{
    public FullTextSearchRecord()
    {
    }

    public FullTextSearchRecord(int RowId, string Match, double? Rank)
    {
        this.RowId = RowId;
        this.Match = Match;
        this.Rank = Rank;
    }

    [Column(SkipOnInsert = true)]
    public int RowId { get; init; }

    [Column(SkipOnInsert = true)]
    public string Match { get; init; } = null!;

    [Column(SkipOnInsert = true)]
    public double? Rank { get; init; }
}
