using MiniLcm;
using LinqToDB;

namespace LcmCrdt;

public static class SqlHelpers
{
    [Sql.Expression("exists(select 1 from json_each({0}, '$.Values') where value = {1})",
        PreferServerSide = true,
        IsPredicate = true)]
    public static bool HasValue(this MultiString ms, string value)
    {
        return ms.Values.Values.Contains(value);
    }

    [Sql.Expression("exists(select 1 from json_each({0}, '$.Values') where value like '%' || {1} || '%')",
        PreferServerSide = true,
        IsPredicate = true)]
    public static bool SearchValue(this MultiString ms, string value)
    {
        return ms.Values.Values.Contains(value);
    }
}
