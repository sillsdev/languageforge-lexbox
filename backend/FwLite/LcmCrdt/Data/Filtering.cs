using System.Linq.Expressions;
using MiniLcm;

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
