namespace LcmCrdt;

public static class QueryHelpers
{
    public static void DefaultOrder(this Entry entry)
    {
        entry.Senses = entry.Senses.DefaultOrder().ToList();
    }
    public static IEnumerable<T> DefaultOrder<T>(this IEnumerable<T> queryable) where T : IOrderable
    {
        return queryable
            .OrderBy(e => e.Order)
            .ThenBy(e => e.Id);
    }
}
