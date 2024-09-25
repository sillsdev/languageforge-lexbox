using LinqToDB;
using MiniLcm.Models;

namespace LcmCrdt;

public static class SqlHelpers
{
    [Sql.Expression("exists(select 1 from json_each({0}, '$') where value = {1})",
        PreferServerSide = true,
        IsPredicate = true)]
    public static bool HasValue(this MultiString ms, string value)
    {
        return ms.Values.Values.Contains(value);
    }

    [Sql.Expression("exists(select 1 from json_each({0}, '$') where value like '%' || {1} || '%')",
        PreferServerSide = true,
        IsPredicate = true)]
    public static bool SearchValue(this MultiString ms, string search)
    {
        return ms.Values.Any(pair => pair.Value.Contains(search));
    }
}
