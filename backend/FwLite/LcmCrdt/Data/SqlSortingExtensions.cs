using System.Linq.Expressions;
using LinqToDB;

namespace LcmCrdt.Data;

public static class SqlSortingExtensions
{
    public const string CollateUnicodeNoCase = "NOCASE_UNICODE";

    [ExpressionMethod(nameof(CollateUnicodeExpression))]
    internal static string CollateUnicode(this string value, WritingSystemId wsId)
    {
        //could optionally just return the value here, but it would work differently than sql
        throw new InvalidOperationException("CollateUnicode is a LinqToDB only API.");
    }

    private static Expression<Func<string, WritingSystemId, string>> CollateUnicodeExpression()
    {
        //todo maybe in the future we use a custom collation based on the writing system
        return (s, wsId) => s.Collate(CollationName(wsId));
    }

    internal static string CollationName(WritingSystemId wsId)
    {
        //don't use ':' in the name, it won't work
        //'-' is not allowed in a collation name according to linq2db, blocked on bug https://github.com/linq2db/linq2db/issues/4849
        return $"NOCASE_WS_{wsId.Code.Replace("-", "_")}";
    }
}
