namespace LcmCrdt.Data;

public static class Sorting
{
    /// <summary>
    /// crude emulation of FTS search relevance
    /// </summary>
    public static IQueryable<Entry> ApplyRoughBestMatchOrder(this IQueryable<Entry> entries, SortOptions order, string? query = null)
    {
        if (order.Ascending)
        {
            return entries
                .OrderByDescending(e => !string.IsNullOrEmpty(query) && SqlHelpers.ContainsIgnoreCaseAccents(e.Headword(order.WritingSystem), query!))
                .ThenBy(e => e.Headword(order.WritingSystem).Length)
                .ThenBy(e => e.Headword(order.WritingSystem))
                //.ThenBy(e => e.MorphType.SecondaryOrder)
                .ThenBy(e => e.Id);
        }
        else
        {
            return entries
                .OrderBy(e => !string.IsNullOrEmpty(query) && SqlHelpers.ContainsIgnoreCaseAccents(e.Headword(order.WritingSystem), query!))
                .ThenByDescending(e => e.Headword(order.WritingSystem).Length)
                .ThenByDescending(e => e.Headword(order.WritingSystem))
                //.ThenByDescending(e => e.MorphType.SecondaryOrder)
                .ThenByDescending(e => e.Id);
        }
    }
}
