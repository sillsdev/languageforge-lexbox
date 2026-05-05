using LinqToDB;

namespace LcmCrdt.Data;

public static class Sorting
{
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
                    // entry.HomographNumber,
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
                    // entry.HomographNumber descending,
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
                    // e.HomographNumber,
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
                    // e.HomographNumber descending,
                    e.Id descending
                select e;
        }
    }
}
