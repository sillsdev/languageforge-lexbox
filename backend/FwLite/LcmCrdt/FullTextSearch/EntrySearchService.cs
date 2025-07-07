using System.Globalization;
using System.Linq.Expressions;
using System.Text;
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

    public IQueryable<Entry> FilterAndRank(IQueryable<Entry> queryable, string query, bool rankResults, bool orderAscending)
    {
        //starting from EntrySearchRecordsTable rather than queryable otherwise linq2db loses track of the table
        var filtered = from searchRecord in EntrySearchRecordsTable
            from entry in queryable.InnerJoin(r => r.Id == searchRecord.Id)
            where Sql.Ext.SQLite().Match(searchRecord, query) && (entry.LexemeForm.SearchValue(query)
                                                                  || entry.CitationForm.SearchValue(query)
                                                                  || entry.Senses.Any(s => s.Gloss.SearchValue(query)))
            select new { entry, searchRecord };
        if (rankResults)
        {
            if (orderAscending)
            {
                filtered = filtered.OrderBy(t => Sql.Ext.SQLite().Rank(t.searchRecord)).ThenBy(t => t.entry.Id);
            }
            else
            {
                filtered = filtered.OrderByDescending(t => Sql.Ext.SQLite().Rank(t.searchRecord)).ThenBy(t => t.entry.Id);
            }
        }
        return filtered.Select(t => t.entry);
    }

    public bool ValidSearchTerm(string query) => query.Normalize(NormalizationForm.FormC).Length >= 3;

    public static string? Best(MultiString ms, WritingSystem[] wss, WritingSystemType type)
    {
        return wss.Where(ws => ws.Type == type)
            .Select(ws => ms.Values.TryGetValue(ws.WsId, out var value) ? value : null)
            .FirstOrDefault(v => v is not null);
    }

    public static string? Best(RichMultiString ms, WritingSystem[] wss, WritingSystemType type)
    {
        return wss.Where(ws => ws.Type == type)
            .Select(ws => ms.TryGetValue(ws.WsId, out var value) ? value : null)
             .FirstOrDefault(v => v is not null)?.GetPlainText();
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
            entry.Senses.SelectMany(s => JoinAll(s.Gloss, wss, WritingSystemType.Analysis))
                .Where(d => !string.IsNullOrEmpty(d)));
    }

    public static string Definition(WritingSystem[] wss, Entry entry)
    {
        return string.Join(" ",
            entry.Senses.SelectMany(s => JoinAll(s.Definition, wss, WritingSystemType.Analysis))
                .Where(rt => !rt.IsEmpty)
                .Select(rt => rt.GetPlainText()));
    }

    private static IEnumerable<string> JoinAll(MultiString ms, WritingSystem[] wss, WritingSystemType type)
    {
        return wss.Where(ws => ws.Type == type)
            .Select(ws => ms.Values.TryGetValue(ws.WsId, out var value) ? value : null)
            .OfType<string>()
            .Where(v => v.Length > 0);
    }

    private static IEnumerable<RichString> JoinAll(RichMultiString ms, WritingSystem[] wss, WritingSystemType type)
    {
        return wss.Where(ws => ws.Type == type)
            .Select(ws => ms.TryGetValue(ws.WsId, out var value) ? value : null)
            .OfType<RichString>();
    }

    public async Task UpdateEntrySearchTable(Guid entryId)
    {
        var entry = await dbContext.GetTable<Entry>()
            .LoadWith(e => e.Senses)
            .AsQueryable()
            .FirstOrDefaultAsync(e => e.Id == entryId);
        if (entry is not null)
        {
            await UpdateEntrySearchTable(entry);
        }
        else
        {
            logger.LogWarning("Entry {EntryId} not found in database.", entryId);
        }
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
        await UpdateEntrySearchTable(entries, [], dbContext);
    }

    public static async Task UpdateEntrySearchTable(IEnumerable<Entry> entries, IEnumerable<WritingSystem> newWritingSystems, LcmCrdtDbContext dbContext)
    {
        WritingSystem[] writingSystems = [
            ..dbContext.WritingSystems,
            ..newWritingSystems
        ];
        Array.Sort(writingSystems, (ws1, ws2) => ws1.Order.CompareTo(ws2.Order));
        await dbContext.GetTable<EntrySearchRecord>()
            .BulkCopyAsync(entries.Select(entry => ToEntrySearchRecord(entry, writingSystems)));
    }

    public async Task RegenerateEntrySearchTable()
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        await EntrySearchRecordsTable.TruncateAsync();

        var writingSystems = await dbContext.WritingSystems.OrderBy(ws => ws.Order).ToArrayAsync();
        await EntrySearchRecordsTable
            .BulkCopyAsync(dbContext.Set<Entry>()
                .LoadWith(e => e.Senses)
                .AsQueryable()
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

    public async Task RemoveSearchRecord(Guid entryId)
    {
        await EntrySearchRecordsTable.DeleteAsync(e => e.Id == entryId);
    }
}
