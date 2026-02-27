namespace LcmCrdt.Data;

public static class Sorting
{
    /// <summary>
    /// Rough search relevance for when FTS is unavailable. Headword matches come first, preferring
    /// prefix matches (e.g. when searching "tan" then "tanan" is before "matan"), then shorter, then alphabetical.
    /// See also: <see cref="FullTextSearch.EntrySearchService.FilterAndRank"/> for the FTS-based equivalent.
    /// </summary>
    public static IQueryable<Entry> ApplyRoughBestMatchOrder(this IQueryable<Entry> entries, SortOptions order, string? query = null)
    {
        if (order.Ascending)
        {
            return entries
                .OrderByDescending(e => !string.IsNullOrEmpty(query) && SqlHelpers.ContainsIgnoreCaseAccents(e.Headword(order.WritingSystem), query!))
                .ThenByDescending(e => !string.IsNullOrEmpty(query) && SqlHelpers.StartsWithIgnoreCaseAccents(e.Headword(order.WritingSystem), query!))
                .ThenBy(e => e.Headword(order.WritingSystem).Length)
                .ThenBy(e => e.Headword(order.WritingSystem))
                .ThenBy(e => e.Id);
        }
        else
        {
            return entries
                .OrderBy(e => !string.IsNullOrEmpty(query) && SqlHelpers.ContainsIgnoreCaseAccents(e.Headword(order.WritingSystem), query!))
                .ThenBy(e => !string.IsNullOrEmpty(query) && SqlHelpers.StartsWithIgnoreCaseAccents(e.Headword(order.WritingSystem), query!))
                .ThenByDescending(e => e.Headword(order.WritingSystem).Length)
                .ThenByDescending(e => e.Headword(order.WritingSystem))
                .ThenByDescending(e => e.Id);
        }
    }
}
