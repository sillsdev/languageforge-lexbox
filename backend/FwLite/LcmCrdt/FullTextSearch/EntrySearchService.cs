using System.Linq.Expressions;
using LcmCrdt.Data;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LcmCrdt.FullTextSearch;

public class EntrySearchService(LcmCrdtDbContext dbContext, ILogger<EntrySearchService> logger)
{
    internal IQueryable<EntrySearchRecord> EntrySearchRecords => dbContext.Set<EntrySearchRecord>();

    //ling2db table
    private ITable<EntrySearchRecord> EntrySearchRecordsTable => dbContext.GetTable<EntrySearchRecord>();

    public Expression<Func<Entry, bool>> SearchFilter(string query)
    {
        if (query.Length < 3) return Filtering.SearchFilter(query);
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
        var record = ToEntrySearchRecord(entry, writingSystems);
        await EntrySearchRecordsTable.InsertOrUpdateAsync(() => new EntrySearchRecord()
            {
                Id = record.Id,
                Headword = record.Headword,
                LexemeForm = record.LexemeForm,
                CitationForm = record.CitationForm,
                Definition = record.Definition,
                Gloss = record.Gloss,
            },
            exiting => new EntrySearchRecord()
            {
                Id = record.Id,
                Headword = record.Headword,
                LexemeForm = record.LexemeForm,
                CitationForm = record.CitationForm,
                Definition = record.Definition,
                Gloss = record.Gloss,
            });
    }

    public async Task UpdateEntrySearchTable(IEnumerable<Entry> entries)
    {
        var writingSystems = await dbContext.WritingSystems.OrderBy(ws => ws.Order).ToArrayAsync();
        await EntrySearchRecordsTable
            .BulkCopyAsync(entries.Select(entry => ToEntrySearchRecord(entry, writingSystems)));
    }

    public async Task RegenerateEntrySearchTable()
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        await EntrySearchRecordsTable.TruncateAsync();

        var writingSystems = await dbContext.WritingSystems.OrderBy(ws => ws.Order).ToArrayAsync();
        await EntrySearchRecordsTable
            .BulkCopyAsync(dbContext.Set<Entry>()
                .Select(entry => ToEntrySearchRecord(entry, writingSystems))
                .AsAsyncEnumerable());
        await transaction.CommitAsync();
    }

    public async Task RegenerateIfMissing()
    {
        if (await QueryMissingEntries().AnyAsync())
        {
            logger.LogWarning("Regenerating entry search table because it is missing entries. This may take a while.");
            await RegenerateEntrySearchTable();
        }
    }

    public IAsyncEnumerable<Entry> EntriesMissingInSearchTable()
    {
        return QueryMissingEntries()
            .AsAsyncEnumerable();
    }

    private IQueryable<Entry> QueryMissingEntries()
    {
        return dbContext.Set<Entry>()
            .Where(e => !EntrySearchRecords.Any(esr => esr.Id == e.Id));
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
