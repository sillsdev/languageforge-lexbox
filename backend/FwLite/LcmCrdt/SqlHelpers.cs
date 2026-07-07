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
        return ms.Values.Any(pair => !pair.Key.IsAudio && pair.Value.ContainsDiacriticMatch(search));
    }

    private static Expression<Func<MultiString, string, bool>> SearchValueExpression()
    {
        return (ms, search) => Json.QueryEntries(ms).Any(kv => !IsAudioWs(kv.Key) && ContainsIgnoreCaseAccents(kv.Value, search));
    }

    /// <summary>
    /// An audio writing system's IETF tag uses the Zxxx ("unwritten") script with an "audio"
    /// private-use variant (see <see cref="WritingSystemId.IsAudio"/>). Its stored value is a
    /// media-file reference, so it must be excluded from text searches.
    /// </summary>
    [Sql.Expression("({0} LIKE '%-zxxx-%' AND {0} LIKE '%-audio%')", ServerSideOnly = true, IsPredicate = true)]
    public static bool IsAudioWs(string wsCode) => new WritingSystemId(wsCode).IsAudio;

    [Sql.Expression(CustomSqliteFunctionInterceptor.ContainsFunction + "({0}, {1})")]
    public static bool ContainsIgnoreCaseAccents(string s, string search) => s.ContainsDiacriticMatch(search);

    [Sql.Expression(CustomSqliteFunctionInterceptor.StartsWithFunction + "({0}, {1})")]
    public static bool StartsWithIgnoreCaseAccents(string s, string search) => s.StartsWithDiacriticMatch(search);

    [Sql.Expression("({0} || {1} || {2})", PreferServerSide = true)]
    public static string ConcatTokens(string leading, string value, string trailing) => leading + value + trailing;
}
