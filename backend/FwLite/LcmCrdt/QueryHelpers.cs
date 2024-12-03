namespace LcmCrdt;

public static class QueryHelpers
{
    public static IEnumerable<T> DefaultOrder<T>(this IEnumerable<T> queryable) where T : IOrderable
    {
        return queryable
            .OrderBy(e => e.Order)
            .ThenBy(e => e.Id);
    }
}
