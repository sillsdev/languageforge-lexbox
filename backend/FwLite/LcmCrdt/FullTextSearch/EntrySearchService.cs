using System.Diagnostics.CodeAnalysis;
using LcmCrdt.Data;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.EntityFrameworkCore;
using LinqToDB.Mapping;

namespace LcmCrdt.FullTextSearch;

public record EntrySearchRecord : FullTextSearchRecord
{
    public EntrySearchRecord()
    {
    }


    public Guid Id { get; init; }
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

    public void Deconstruct(out int RowId, out string Match, out double? Rank)
    {
        RowId = this.RowId;
        Match = this.Match;
        Rank = this.Rank;
    }
}

public class EntrySearchService(LcmCrdtDbContext dbContext)
{
    public string? Best(MultiString ms, WritingSystem[] wss, WritingSystemType type)
    {
        return wss.Where(ws => ws.Type == type)
            .Select(ws => ms.Values.TryGetValue(ws.WsId, out var value) ? value : null)
            .FirstOrDefault();
    }

    public string? Best(RichMultiString ms, WritingSystem[] wss, WritingSystemType type)
    {
        return wss.Where(ws => ws.Type == type)
            .Select(ws => ms.TryGetValue(ws.WsId, out var value) ? value : null)
            .FirstOrDefault()?.GetPlainText();
    }

    public string Definition(WritingSystem[] wss, Entry entry)
    {
        return string.Join(" ", entry.Senses.Select(s => Best(s.Definition, wss, WritingSystemType.Analysis)).Where(d => !string.IsNullOrEmpty(d)));
    }

    public string LexemeForm(WritingSystem[] wss, Entry entry)
    {
        return string.Join(" ",
            wss.Where(ws => ws.Type == WritingSystemType.Vernacular)
                .Select(ws => entry.LexemeForm[ws.WsId])
                .Where(h => !string.IsNullOrEmpty(h)));
    }

    public string CitationForm(WritingSystem[] wss, Entry entry)
    {
        return string.Join(" ",
            wss.Where(ws => ws.Type == WritingSystemType.Vernacular)
                .Select(ws => entry.CitationForm[ws.WsId])
                .Where(h => !string.IsNullOrEmpty(h)));
    }

    public string Gloss(WritingSystem[] wss, Entry entry)
    {
        return string.Join(" ",
            entry.Senses.Select(s => Best(s.Gloss, wss, WritingSystemType.Analysis))
                .Where(d => !string.IsNullOrEmpty(d)));
    }

    public async Task UpdateEntrySearchTable(Entry entry)
    {
        var ws = await dbContext.WritingSystems.OrderBy(ws => ws.Order).ToArrayAsync();
        await dbContext.EntrySearchRecords.ToLinqToDBTable().InsertAsync(() => new EntrySearchRecord()
        {
            Id = entry.Id,
            LexemeForm = LexemeForm(ws, entry),
            CitationForm = CitationForm(ws, entry),
            Definition = Definition(ws, entry),
            Gloss = Gloss(ws, entry)
        });
    }

    public async Task UpdateEntrySearchTable(IEnumerable<Entry> entries)
    {
        var ws = await dbContext.WritingSystems.OrderBy(ws => ws.Order).ToArrayAsync();
        bool created = false;
        await dbContext.EntrySearchRecords.ToLinqToDBTable()
            .BulkCopyAsync(entries.Select(entry =>
                new EntrySearchRecord()
                {
                    Id = entry.Id,
                    LexemeForm = LexemeForm(ws, entry),
                    CitationForm = CitationForm(ws, entry),
                    Definition = Definition(ws, entry),
                    Gloss = Gloss(ws, entry)
                }));
    }

    public IAsyncEnumerable<EntrySearchRecord> Search(string query)
    {
        return dbContext.EntrySearchRecords
            .ToLinqToDB()
            .Where(e => Sql.Ext.SQLite().Match(e, query))
            .OrderBy(e => Sql.Ext.SQLite().Rank(e))
            .AsAsyncEnumerable();
    }
}
