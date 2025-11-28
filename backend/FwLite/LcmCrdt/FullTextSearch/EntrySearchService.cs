using System.Text;
using LcmCrdt.Data;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LcmCrdt.FullTextSearch;

public class EntrySearchServiceFactory(
    Microsoft.EntityFrameworkCore.IDbContextFactory<LcmCrdtDbContext> dbContextFactory,
    IServiceProvider serviceProvider)
{
    public EntrySearchService CreateSearchService(LcmCrdtDbContext? dbContext = null)
    {
        return ActivatorUtilities.CreateInstance<EntrySearchService>(serviceProvider,
            dbContext ?? dbContextFactory.CreateDbContext(),
            dbContext is null);
    }
}

public class EntrySearchService(LcmCrdtDbContext dbContext, ILogger<EntrySearchService> logger, bool disposeOfDbContext)
    : IAsyncDisposable, IDisposable
{
    internal IQueryable<EntrySearchRecord> EntrySearchRecords => dbContext.Set<EntrySearchRecord>();

    //linq2db table
    private ITable<EntrySearchRecord> EntrySearchRecordsTable => dbContext.GetTable<EntrySearchRecord>();

    public IQueryable<Entry> FilterAndRank(IQueryable<Entry> queryable,
        string query,
        bool rankResults)
    {
        var ftsString = ToFts5LiteralString(query);

        //starting from EntrySearchRecordsTable rather than queryable otherwise linq2db loses track of the table
        var filtered = from searchRecord in EntrySearchRecordsTable
            from entry in queryable.InnerJoin(r => r.Id == searchRecord.Id)
            where Sql.Ext.SQLite().Match(searchRecord, ftsString) && (entry.LexemeForm.SearchValue(query)
                                                                  || entry.CitationForm.SearchValue(query)
                                                                  || entry.Senses.Any(s => s.Gloss.SearchValue(query)))
            select new { entry, searchRecord };
        if (rankResults)
        {
            filtered = filtered.OrderBy(t => Sql.Ext.SQLite().Rank(t.searchRecord)).ThenBy(t => t.entry.Id);
        }

        return filtered.Select(t => t.entry);
    }

    private static string ToFts5LiteralString(string query)
    {
        // https://sqlite.org/fts5.html#fts5_strings
        // - escape double quotes by doubling them
        // - wrap the entire query in quotes
        return $"\"{query.Replace("\"", "\"\"")}\"";
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
        var writingSystems = await dbContext.WritingSystemsOrdered.ToArrayAsync();
        var record = ToEntrySearchRecord(entry, writingSystems);
        await InsertOrUpdateEntrySearchRecord(record, EntrySearchRecordsTable);
    }

    private static async Task InsertOrUpdateEntrySearchRecord(EntrySearchRecord record, ITable<EntrySearchRecord> table)
    {
        await table.InsertOrUpdateAsync(() => new EntrySearchRecord()
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
        await UpdateEntrySearchTable(entries, [], [], dbContext);
    }

    public static async Task UpdateEntrySearchTable(IEnumerable<Entry> entries,
        IEnumerable<Guid> removed,
        IEnumerable<WritingSystem> newWritingSystems,
        LcmCrdtDbContext dbContext)
    {
        WritingSystem[] writingSystems =
        [
            ..dbContext.WritingSystems,
            ..newWritingSystems
        ];
        Array.Sort(writingSystems, (ws1, ws2) =>
        {
            var orderComparison = ws1.Order.CompareTo(ws2.Order);
            if (orderComparison != 0) return orderComparison;
            return ws1.Id.CompareTo(ws2.Id);
        });
        var entrySearchRecordsTable = dbContext.GetTable<EntrySearchRecord>();
        var searchRecords = entries.Select(entry => ToEntrySearchRecord(entry, writingSystems));
        foreach (var entrySearchRecord in searchRecords)
        {
            //can't use bulk copy here because that creates duplicate rows
            await InsertOrUpdateEntrySearchRecord(entrySearchRecord, entrySearchRecordsTable);
        }

        await entrySearchRecordsTable
            .Where(e => removed.Contains(e.Id))
            .DeleteAsync();
    }

    public async Task RegenerateEntrySearchTable()
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        await EntrySearchRecordsTable.TruncateAsync();

        var writingSystems = await dbContext.WritingSystemsOrdered.ToArrayAsync();
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
        if (await HasMissingEntries())
        {
            logger.LogWarning("Regenerating entry search table because it is missing entries. This may take a while.");
            await RegenerateEntrySearchTable();
        }
    }

    private async Task<bool> HasMissingEntries()
    {
        //not using a query to check Ids because the Id on the search table isn't indexed so it will be slow
        return await EntrySearchRecordsTable.CountAsync() != await dbContext.Set<Entry>().CountAsync();
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
        // (Currently only used by tests)
        // This method is for advanced queries with FTS5 syntax (wildcards, operators, etc.).
        // So, we don't use ToFts5LiteralString.
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

    public async ValueTask DisposeAsync()
    {
        if (disposeOfDbContext)
            await dbContext.DisposeAsync();
    }

    public void Dispose()
    {
        if (disposeOfDbContext)
            dbContext.Dispose();
    }
}
