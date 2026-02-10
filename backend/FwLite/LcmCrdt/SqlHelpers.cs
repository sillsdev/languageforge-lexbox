using System.Globalization;
using System.Linq.Expressions;
using LcmCrdt.Data;
using LinqToDB;
using LinqToDB.DataProvider.SQLite;
using MiniLcm.Culture;

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
        return ms.Values.Any(pair => pair.Value.ContainsDiacriticMatch(search));
    }

    private static Expression<Func<MultiString, string, bool>> SearchValueExpression()
    {
        return (ms, search) => Json.QueryValues(ms).Any(s => ContainsIgnoreCaseAccents(s, search));
    }

    [Sql.Expression(CustomSqliteFunctionInterceptor.ContainsFunction + "({0}, {1})")]
    public static bool ContainsIgnoreCaseAccents(string s, string search) => s.ContainsDiacriticMatch(search);

    [Sql.Expression(CustomSqliteFunctionInterceptor.StartsWithFunction + "({0}, {1})")]
    public static bool StartsWithIgnoreCaseAccents(string s, string search) => s.StartsWithDiacriticMatch(search);
}
