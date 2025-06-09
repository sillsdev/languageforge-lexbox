using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.DataProvider.SQLite;

namespace LcmCrdt.Data;

public static class Filtering
{
    public static IQueryable<Entry> WhereExemplar(
        this IQueryable<Entry> query,
        WritingSystemId ws,
        string exemplar)
    {
        return query.Where(e => e.Headword(ws).StartsWith(exemplar));
    }

    public static Expression<Func<Entry, bool>> SearchFilter(string query)
    {
        return e => e.LexemeForm.SearchValue(query)
                    || e.CitationForm.SearchValue(query)
                    || e.Senses.Any(s => s.Gloss.SearchValue(query));
    }

    public static Expression<Func<Entry, bool>> FtsFilter(string query, LcmCrdtDbContext db)
    {
        return e => db.EntrySearchRecords
            .Where(fts => Sql.Ext.SQLite().Match(fts, query))
            .Select(fts => fts.Id)
            .Contains(e.Id);
    }

    public static Func<Entry, bool> CompiledFilter(string? query, WritingSystemId ws, string? exemplar)
    {
        query = string.IsNullOrEmpty(query) ? null : query;
        return (query, exemplar) switch
        {
            (null, null) => _ => true,
            (not null, null) => e => e.LexemeForm.SearchValue(query)
                                     || e.CitationForm.SearchValue(query)
                                     || e.Senses.Any(s => s.Gloss.SearchValue(query)),
            (null, not null) => e => e.Headword(ws).StartsWith(exemplar),
            (_, _) => e => e.Headword(ws).StartsWith(exemplar)
                           && (e.LexemeForm.SearchValue(query)
                               || e.CitationForm.SearchValue(query)
                               || e.Senses.Any(s => s.Gloss.SearchValue(query)))
        };
    }
}
