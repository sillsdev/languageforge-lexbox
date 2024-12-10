namespace LcmCrdt;

public static class QueryHelpers
{
    public static void ApplySortOrder(this Entry entry)
    {
        entry.Senses.ApplySortOrder();
    }

    public static void ApplySortOrder<T>(this List<T> items) where T : IOrderable
    {
        items.Sort((x, y) =>
        {
            var result = x.Order.CompareTo(y.Order);
            if (result == 0)
            {
                result = x.Id.CompareTo(y.Id);
            }
            return result;
        });
    }
}
