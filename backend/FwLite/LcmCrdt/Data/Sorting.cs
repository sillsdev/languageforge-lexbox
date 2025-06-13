namespace LcmCrdt.Data;

public static class Sorting
{
    public static IQueryable<Entry> ApplyRoughBestMatchOrder(this IQueryable<Entry> entries, SortOptions order, string? query = null)
    {
        if (order.Ascending)
        {
            return entries
                .OrderByDescending(e => !string.IsNullOrEmpty(query) && SqlHelpers.ContainsIgnoreCaseAccents(e.Headword(order.WritingSystem), query!))
                .ThenBy(e => e.Headword(order.WritingSystem).Length)
                .ThenBy(e => e.Headword(order.WritingSystem))
                .ThenBy(e => e.Id);
        }
        else
        {
            return entries
                .OrderBy(e => !string.IsNullOrEmpty(query) && SqlHelpers.ContainsIgnoreCaseAccents(e.Headword(order.WritingSystem), query!))
                .ThenByDescending(e => e.Headword(order.WritingSystem).Length)
                .ThenByDescending(e => e.Headword(order.WritingSystem))
                .ThenByDescending(e => e.Id);
        }
    }
}
