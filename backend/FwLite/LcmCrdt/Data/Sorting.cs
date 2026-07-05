using LinqToDB;

namespace LcmCrdt.Data;

/// <summary>
/// A sense-row key pair as ordered by <see cref="Sorting.ApplyGlossOrder"/>;
/// hydrated into <see cref="EntrySenseRow"/> after paging.
/// </summary>
public record EntrySenseRowKey(Guid EntryId, Guid? SenseId);

public static class Sorting
{
    /// <summary>
    /// Expands entries to one row per sense (left join, so an entry without senses keeps a single row)
    /// ordered by gloss. Rows without a gloss in the sort writing system always sort last and
    /// tiebreakers always ascend, so only the gloss order itself flips with <see cref="SortOptions.Ascending"/>.
    /// </summary>
    public static IQueryable<EntrySenseRowKey> ApplyGlossOrder(
        this IQueryable<Entry> entries,
        ITable<Sense> senses,
        ITable<MorphType> morphTypes,
        SortOptions order,
        WritingSystemId headwordWs)
    {
        var glossWs = order.WritingSystem;
        var stemOrder = morphTypes.Where(m => m.Kind == MorphTypeKind.Stem).Select(m => m.SecondaryOrder);
        if (order.Ascending)
        {
            return
                from e in entries
                join sense in senses on e.Id equals sense.EntryId into senseGroup
                from s in senseGroup.DefaultIfEmpty()
                orderby
                    string.IsNullOrEmpty(Json.Value(s.Gloss, ms => ms[glossWs])),
                    Json.Value(s.Gloss, ms => ms[glossWs])!.CollateUnicode(glossWs),
                    e.Headword(headwordWs).CollateUnicode(headwordWs),
                    morphTypes.Where(m => m.Kind == e.MorphType)
                        .Select(m => (int?)m.SecondaryOrder).FirstOrDefault() ?? stemOrder.FirstOrDefault(),
                    e.HomographNumber,
                    e.Id,
                    s.Order,
                    s.Id
                select new EntrySenseRowKey(e.Id, s != null ? (Guid?)s.Id : null);
        }
        else
        {
            return
                from e in entries
                join sense in senses on e.Id equals sense.EntryId into senseGroup
                from s in senseGroup.DefaultIfEmpty()
                orderby
                    string.IsNullOrEmpty(Json.Value(s.Gloss, ms => ms[glossWs])),
                    Json.Value(s.Gloss, ms => ms[glossWs])!.CollateUnicode(glossWs) descending,
                    e.Headword(headwordWs).CollateUnicode(headwordWs),
                    morphTypes.Where(m => m.Kind == e.MorphType)
                        .Select(m => (int?)m.SecondaryOrder).FirstOrDefault() ?? stemOrder.FirstOrDefault(),
                    e.HomographNumber,
                    e.Id,
                    s.Order,
                    s.Id
                select new EntrySenseRowKey(e.Id, s != null ? (Guid?)s.Id : null);
        }
    }

    public static IQueryable<Entry> ApplyHeadwordOrder(this IQueryable<Entry> entries, ITable<MorphType> morphTypes, SortOptions order, string? query = null)
    {
        var stemOrder = morphTypes.Where(m => m.Kind == MorphTypeKind.Stem).Select(m => m.SecondaryOrder);
        if (order.Ascending)
        {
            return
                from entry in entries
                orderby
                    entry.Headword(order.WritingSystem).CollateUnicode(order.WritingSystem),
                    morphTypes.Where(m => m.Kind == entry.MorphType)
                        .Select(m => (int?)m.SecondaryOrder).FirstOrDefault() ?? stemOrder.FirstOrDefault(),
                    entry.HomographNumber,
                    entry.Id
                select entry;
        }
        else
        {
            return
                from entry in entries
                orderby
                    entry.Headword(order.WritingSystem).CollateUnicode(order.WritingSystem) descending,
                    (morphTypes.Where(m => m.Kind == entry.MorphType)
                        .Select(m => (int?)m.SecondaryOrder).FirstOrDefault() ?? stemOrder.FirstOrDefault()) descending,
                    entry.HomographNumber descending,
                    entry.Id descending
                select entry;
        }
    }

    /// <summary>
    /// Rough search relevance for when FTS is unavailable. Headword matches come first, preferring
    /// prefix matches (e.g. when searching "tan" then "tanan" is before "matan"), then shorter, then alphabetical.
    /// See also: <see cref="FullTextSearch.EntrySearchService.FilterAndRank"/> for the FTS-based equivalent.
    /// </summary>
    public static IQueryable<Entry> ApplyRoughBestMatchOrder(this IQueryable<Entry> entries, ITable<MorphType> morphTypes, SortOptions order, string? query = null)
    {
        var stemOrder = morphTypes.Where(m => m.Kind == MorphTypeKind.Stem).Select(m => m.SecondaryOrder);
        if (order.Ascending)
        {
            return
                from e in entries
                join mt in morphTypes on e.MorphType equals mt.Kind into mtGroup
                from mt in mtGroup.DefaultIfEmpty()
                orderby
                    !string.IsNullOrEmpty(query) && SqlHelpers.StartsWithIgnoreCaseAccents(e.HeadwordWithTokens(order.WritingSystem, mt.Prefix, mt.Postfix), query!) descending,
                    !string.IsNullOrEmpty(query) && SqlHelpers.ContainsIgnoreCaseAccents(e.HeadwordWithTokens(order.WritingSystem, mt.Prefix, mt.Postfix), query!) descending,
                    e.Headword(order.WritingSystem).Length,
                    e.Headword(order.WritingSystem),
                    mt != null ? mt.SecondaryOrder : stemOrder.FirstOrDefault(),
                    e.HomographNumber,
                    e.Id
                select e;
        }
        else
        {
            return
                from e in entries
                join mt in morphTypes on e.MorphType equals mt.Kind into mtGroup
                from mt in mtGroup.DefaultIfEmpty()
                orderby
                    !string.IsNullOrEmpty(query) && SqlHelpers.StartsWithIgnoreCaseAccents(e.HeadwordWithTokens(order.WritingSystem, mt.Prefix, mt.Postfix), query!),
                    !string.IsNullOrEmpty(query) && SqlHelpers.ContainsIgnoreCaseAccents(e.HeadwordWithTokens(order.WritingSystem, mt.Prefix, mt.Postfix), query!),
                    e.Headword(order.WritingSystem).Length descending,
                    e.Headword(order.WritingSystem) descending,
                    (mt != null ? mt.SecondaryOrder : stemOrder.FirstOrDefault()) descending,
                    e.HomographNumber descending,
                    e.Id descending
                select e;
        }
    }
}
