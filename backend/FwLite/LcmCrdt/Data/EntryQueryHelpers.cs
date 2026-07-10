using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.DataProvider.SQLite;

namespace LcmCrdt.Data;

public static class EntryQueryHelpers
{
    [ExpressionMethod(nameof(HeadwordExpression))]
    public static string Headword(this Entry e, WritingSystemId ws)
    {
        var citation = e.CitationForm[ws]?.Trim();
        if (!string.IsNullOrEmpty(citation)) return citation;
        return e.LexemeForm[ws]?.Trim() ?? string.Empty;
    }

    private static Expression<Func<Entry, WritingSystemId, string?>> HeadwordExpression() =>
        (e, ws) => string.IsNullOrEmpty((Json.Value(e.CitationForm, ms => ms[ws]) ?? "").Trim())
            ? (Json.Value(e.LexemeForm, ms => ms[ws]) ?? "").Trim()
            : (Json.Value(e.CitationForm, ms => ms[ws]) ?? "").Trim();

    /// <summary>
    /// https://sqlite.org/lang_corefunc.html#concat_ws
    /// like string.join, but if the separator is null, it returns null
    /// </summary>
    [Sql.Function("concat_ws", ServerSideOnly = true, ArgIndices = [1, 2, 3])]
    public static string? ConcatWs(this ISQLiteExtensions? ext, string? sep, string? val1, string? val2)
    {
        if (sep is null) return null;
        return string.Join(sep, val1, val2);
    }

    /// <summary>
    /// https://sqlite.org/lang_corefunc.html#nullif
    /// returns the value, unless it's equal to other, then it returns null
    /// </summary>
    [Sql.Function("nullif", ServerSideOnly = true, ArgIndices = [1, 2])]
    public static T? NullIf<T>(this ISQLiteExtensions? ext, T? value, T? other) =>
        (value is null && other is null) || value?.Equals(other) == true ? default : value;

    public static MorphType? QueryMorphType(this Entry e) => throw new NotSupportedException();
    public static Entry? QueryComponentEntry(ComplexFormComponent c) => throw new NotSupportedException();
    public static Sense? QueryComponentSense(ComplexFormComponent c) => throw new NotSupportedException();
    public static Entry? QueryComplexFormEntry(ComplexFormComponent c) => throw new NotSupportedException();

    [Sql.Expression("""
                    (select WsId
                    from WritingSystem
                    where Type = {0}
                    order by "Order", Id
                    limit 1)
                    """,
        ServerSideOnly = true)]
    public static WritingSystemId DefaultWritingSystem(WritingSystemType type) => throw new NotSupportedException();

    [ExpressionMethod(nameof(QueryHeadwordWithTokensExpression))]
    public static string QueryHeadwordWithTokens(this Entry e, WritingSystemId ws) => throw new NotSupportedException();

    private static Expression<Func<Entry, WritingSystemId, string>> QueryHeadwordWithTokensExpression() =>
        (e, ws) => e.HeadwordWithTokens(ws, e.QueryMorphType());

    [ExpressionMethod(nameof(HeadwordFromMorphTypeExpression))]
    public static string HeadwordWithTokens(this Entry e, WritingSystemId ws, MorphType? morphType)
    {
        var citation = e.CitationForm[ws]?.Trim();
        if (!string.IsNullOrEmpty(citation)) return citation;
        var lexeme = e.LexemeForm[ws]?.Trim();
        if (string.IsNullOrEmpty(lexeme)) return string.Empty;
        return ((morphType?.Prefix ?? "") + lexeme + (morphType?.Postfix ?? "")).Trim();
    }

    private static Expression<Func<Entry, WritingSystemId, MorphType?, string>> HeadwordFromMorphTypeExpression() =>
        (e, ws, morphType) => e.HeadwordWithTokens(ws, morphType!.Prefix, morphType!.Postfix);

    [ExpressionMethod(nameof(HeadwordWithTokensExpression))]
    public static string HeadwordWithTokens(this Entry e, WritingSystemId ws, string? leading, string? trailing)
    {
        var citation = e.CitationForm[ws]?.Trim();
        if (!string.IsNullOrEmpty(citation)) return citation;
        var lexeme = e.LexemeForm[ws]?.Trim();
        if (string.IsNullOrEmpty(lexeme)) return string.Empty;
        return ((leading ?? "") + lexeme + (trailing ?? "")).Trim();
    }

    private static Expression<Func<Entry, WritingSystemId, string?, string?, string?>> HeadwordWithTokensExpression() =>
        (e, ws, leading, trailing) =>
            Sql.Ext.SQLite().NullIf(Json.Value(e.CitationForm, ms => ms[ws])!.Trim(), "")
            //hack using concat_ws which if the first parameter is null it will return null, if not it will join leading and trailing with the separator
            //there's a bug in the version of sqlite that we're using https://sqlite.org/forum/forumpost/52503ac21d
            //so instead of leading ?? "" we must use leading ?? " " and trim off the trailing space, same for trailing
            ?? Sql.Ext.SQLite().ConcatWs(Sql.Ext.SQLite().NullIf(Json.Value(e.LexemeForm, ms => ms[ws])!.Trim(), ""), leading ?? " ", trailing ?? " ")!.Trim();

    [ExpressionMethod(nameof(SearchHeadwords))]
    public static bool SearchHeadwords(this Entry e, string? leading, string? trailing, string query)
    {
        return e.CitationForm.SearchValue(query)
            || e.LexemeForm.Values.Any(kvp =>
                !kvp.Key.IsAudio &&
                string.IsNullOrEmpty(e.CitationForm[kvp.Key]?.Trim()) &&
                SqlHelpers.ContainsIgnoreCaseAccents((leading ?? "") + (kvp.Value?.Trim() ?? "") + (trailing ?? ""), query));
    }

    private static Expression<Func<Entry, string?, string?, string, bool>> SearchHeadwords()
    {
        return (e, leading, trailing, query) =>
            Json.QueryEntries(e.CitationForm).Any(
                kv => !SqlHelpers.IsAudioWs(kv.Key) && SqlHelpers.ContainsIgnoreCaseAccents(kv.Value, query)) ||
            Json.QueryEntries(e.LexemeForm).Any(kv =>
                !SqlHelpers.IsAudioWs(kv.Key) &&
                string.IsNullOrEmpty((Json.At(e.CitationForm, kv.Key) ?? "").Trim()) &&
                SqlHelpers.ContainsIgnoreCaseAccents((leading ?? "") + (kv.Value ?? "").Trim() + (trailing ?? ""), query));
    }


    /// <summary>
    /// Computes headwords for all writing systems present in CitationForm or LexemeForm,
    /// applying morph tokens when CitationForm is absent.
    /// Used for in-memory population of Entry.Headword after loading from DB.
    /// </summary>
    public static MultiString ComputeHeadwords(Entry entry,
        IReadOnlyDictionary<MorphTypeKind, MorphType> morphTypeLookup)
    {
        var result = new MultiString();
        morphTypeLookup.TryGetValue(entry.MorphType, out var morphData);

        // Iterate all WS keys that have data, not just "current" vernacular WSs,
        // so we don't lose headwords for non-current or future writing systems.
        var wsIds = entry.CitationForm.Values.Keys
            .Union(entry.LexemeForm.Values.Keys);

        foreach (var wsId in wsIds)
        {
            var citation = entry.CitationForm[wsId]?.Trim();
            if (!string.IsNullOrEmpty(citation))
            {
                result[wsId] = citation;
                continue;
            }

            var lexeme = entry.LexemeForm[wsId]?.Trim();
            if (!string.IsNullOrEmpty(lexeme))
            {
                var leading = morphData?.Prefix ?? "";
                var trailing = morphData?.Postfix ?? "";
                result[wsId] = (leading + lexeme + trailing).Trim();
            }
        }

        return result;
    }
}
