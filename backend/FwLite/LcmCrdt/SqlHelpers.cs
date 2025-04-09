using System.Linq.Expressions;
using LinqToDB;

namespace LcmCrdt;

public static class SqlHelpers
{
    [ExpressionMethod(nameof(HasValueExpression))]
    public static bool HasValue(this MultiString ms, string value)
    {
        return ms.Values.Values.Contains(value);
    }

    private static Expression<Func<MultiString, string, bool>> HasValueExpression()
    {
        return (ms, value) => Json.QueryValues(ms).Contains(value);
    }

    [ExpressionMethod(nameof(SearchValueExpression))]
    public static bool SearchValue(this MultiString ms, string search)
    {
        return ms.Values.Any(pair => pair.Value.Contains(search));
    }

    private static Expression<Func<MultiString, string, bool>> SearchValueExpression()
    {
        return (ms, search) => Json.QueryValues(ms).Any(s => s.Contains(search));
    }
}
