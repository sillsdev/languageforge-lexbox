using System.Diagnostics.CodeAnalysis;
using LcmCrdt.Data;
using LinqToDB;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.EntityFrameworkCore;

namespace LcmCrdt.FullTextSearch;

public record EntrySearchRecord : FullTextSearchRecord
{
    public EntrySearchRecord()
    {
    }

    [SetsRequiredMembers]
    public EntrySearchRecord(Guid Id, string Headword, string gloss, string definition)
    {
        this.Id = Id;
        this.Headword = Headword;
        Gloss = gloss;
        Definition = definition;
    }

    public Guid Id { get; init; }
    public required string Headword { get; init; }
    public required string Gloss { get; init; }
    public required string Definition { get; init; }

    public void Deconstruct(out Guid Id, out string Headword, out int RowId, out string Match, out double? Rank)
    {
        Id = this.Id;
        Headword = this.Headword;
        RowId = this.RowId;
        Match = this.Match;
        Rank = this.Rank;
    }
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

    public int RowId { get; init; }
    public string Match { get; init; } = null!;
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
            .FirstOrDefault();
    }

    public string Definition(WritingSystem[] wss, Entry entry)
    {
        return string.Join("|", entry.Senses.Select(s => Best(s.Definition, wss, WritingSystemType.Analysis)).Where(d => !string.IsNullOrEmpty(d)));
    }

    public string Headword(WritingSystem[] wss, Entry entry)
    {
        return wss.Where(ws => ws.Type == WritingSystemType.Vernacular)
            .Select(ws => entry.Headword(ws.WsId))
            .FirstOrDefault() ?? "???";
    }

    public string Gloss(WritingSystem[] wss, Entry entry)
    {
        return string.Join("|",
            entry.Senses.Select(s => Best(s.Gloss, wss, WritingSystemType.Analysis))
                .Where(d => !string.IsNullOrEmpty(d)));
    }

    public async Task UpdateEntrySearchTable(Entry entry)
    {
        var ws = await dbContext.WritingSystems.OrderBy(ws => ws.Order).ToArrayAsync();
        await dbContext.EntrySearchRecords.ToLinqToDBTable().InsertAsync(() => new EntrySearchRecord()
        {
            Id = entry.Id,
            Headword = Headword(ws, entry),
            Definition = Definition(ws, entry),
            Gloss = Gloss(ws, entry)
        });
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
