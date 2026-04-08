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
    public static bool SearchValue(this MultiString ms, string search, bool matchDiacritics = false)
    {
        return ms.Values.Any(pair => pair.Value.ContainsDiacriticMatch(search, matchDiacritics));
    }

    private static Expression<Func<MultiString, string, bool, bool>> SearchValueExpression()
    {
        return (ms, search, matchDiacritics) => Json.QueryValues(ms).Any(s => ContainsIgnoreCaseAccents(s, search, matchDiacritics));
    }

    [Sql.Expression(CustomSqliteFunctionInterceptor.ContainsFunction + "({0}, {1}, {2})")]
    public static bool ContainsIgnoreCaseAccents(string s, string search, bool matchDiacritics = false) => s.ContainsDiacriticMatch(search, matchDiacritics);

    [Sql.Expression(CustomSqliteFunctionInterceptor.StartsWithFunction + "({0}, {1}, {2})")]
    public static bool StartsWithIgnoreCaseAccents(string s, string search, bool matchDiacritics = false) => s.StartsWithDiacriticMatch(search, matchDiacritics);
}
