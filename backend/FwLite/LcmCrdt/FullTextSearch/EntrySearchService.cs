using System.Linq.Expressions;
using LcmCrdt.Data;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.EntityFrameworkCore;

namespace LcmCrdt.FullTextSearch;

public class EntrySearchService(LcmCrdtDbContext dbContext)
{
    internal IQueryable<EntrySearchRecord> EntrySearchRecords => dbContext.Set<EntrySearchRecord>();

    //ling2db table
    private ITable<EntrySearchRecord> EntrySearchRecordsTable => dbContext.GetTable<EntrySearchRecord>();

    public Expression<Func<Entry, bool>> SearchFilter(string query)
    {
        return Filtering.FtsFilter(query, EntrySearchRecords);
    }

    public static string? Best(MultiString ms, WritingSystem[] wss, WritingSystemType type)
    {
        return wss.Where(ws => ws.Type == type)
            .Select(ws => ms.Values.TryGetValue(ws.WsId, out var value) ? value : null)
            .FirstOrDefault();
    }

    public static string? Best(RichMultiString ms, WritingSystem[] wss, WritingSystemType type)
    {
        return wss.Where(ws => ws.Type == type)
            .Select(ws => ms.TryGetValue(ws.WsId, out var value) ? value : null)
            .FirstOrDefault()?.GetPlainText();
    }

    public static string Definition(WritingSystem[] wss, Entry entry)
    {
        return string.Join(" ",
            entry.Senses.Select(s => Best(s.Definition, wss, WritingSystemType.Analysis))
                .Where(d => !string.IsNullOrEmpty(d)));
    }

    public static string LexemeForm(WritingSystem[] wss, Entry entry)
    {
        return string.Join(" ",
            wss.Where(ws => ws.Type == WritingSystemType.Vernacular)
                .Select(ws => entry.LexemeForm[ws.WsId])
                .Where(h => !string.IsNullOrEmpty(h)));
    }

    public static string CitationForm(WritingSystem[] wss, Entry entry)
    {
        return string.Join(" ",
            wss.Where(ws => ws.Type == WritingSystemType.Vernacular)
                .Select(ws => entry.CitationForm[ws.WsId])
                .Where(h => !string.IsNullOrEmpty(h)));
    }

    public static string Gloss(WritingSystem[] wss, Entry entry)
    {
        return string.Join(" ",
            entry.Senses.Select(s => Best(s.Gloss, wss, WritingSystemType.Analysis))
                .Where(d => !string.IsNullOrEmpty(d)));
    }

    public async Task UpdateEntrySearchTable(Entry entry)
    {
        var writingSystems = await dbContext.WritingSystems.OrderBy(ws => ws.Order).ToArrayAsync();
        await EntrySearchRecordsTable.InsertAsync(() => ToEntrySearchRecord(entry, writingSystems));
    }

    public async Task UpdateEntrySearchTable(IEnumerable<Entry> entries)
    {
        var writingSystems = await dbContext.WritingSystems.OrderBy(ws => ws.Order).ToArrayAsync();
        await EntrySearchRecordsTable
            .BulkCopyAsync(entries.Select(entry => ToEntrySearchRecord(entry, writingSystems)));
    }

    private static EntrySearchRecord ToEntrySearchRecord(Entry entry, WritingSystem[] writingSystems)
    {
        return new EntrySearchRecord()
        {
            Id = entry.Id,
            Headword = entry.Headword(writingSystems.First(ws => ws.Type == WritingSystemType.Vernacular).WsId),
            LexemeForm = LexemeForm(writingSystems, entry),
            CitationForm = CitationForm(writingSystems, entry),
            Definition = Definition(writingSystems, entry),
            Gloss = Gloss(writingSystems, entry)
        };
    }

    public IAsyncEnumerable<EntrySearchRecord> Search(string query)
    {
        return EntrySearchRecords
            .ToLinqToDB()
            .Where(e => Sql.Ext.SQLite().Match(e, query))
            .OrderBy(e => Sql.Ext.SQLite().Rank(e))
            .AsAsyncEnumerable();
    }
}
