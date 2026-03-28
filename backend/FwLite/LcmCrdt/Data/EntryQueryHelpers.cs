using System.Linq.Expressions;
using LinqToDB;

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
            string.IsNullOrEmpty((Json.Value(e.CitationForm, ms => ms[ws]) ?? "").Trim())
                ? string.IsNullOrEmpty((Json.Value(e.LexemeForm, ms => ms[ws]) ?? "").Trim())
                    ? ""
                    : ((leading ?? "") + (Json.Value(e.LexemeForm, ms => ms[ws]) ?? "").Trim() + (trailing ?? "")).Trim()
                : Json.Value(e.CitationForm, ms => ms[ws])!.Trim();

    [ExpressionMethod(nameof(SearchHeadwords))]
    public static bool SearchHeadwords(this Entry e, string? leading, string? trailing, string query)
    {
        return e.CitationForm.SearchValue(query)
            || e.LexemeForm.Values.Any(kvp =>
                string.IsNullOrEmpty(e.CitationForm[kvp.Key]?.Trim()) &&
                SqlHelpers.ContainsIgnoreCaseAccents((leading ?? "") + (kvp.Value?.Trim() ?? "") + (trailing ?? ""), query));
    }

    private static Expression<Func<Entry, string?, string?, string, bool>> SearchHeadwords()
    {
        return (e, leading, trailing, query) =>
            Json.QueryValues(e.CitationForm).Any(
                v => SqlHelpers.ContainsIgnoreCaseAccents(v, query)) ||
            Json.QueryEntries(e.LexemeForm).Any(kv =>
                string.IsNullOrEmpty((Json.Value(e.CitationForm, ms => ms[kv.Key]) ?? "").Trim()) &&
                SqlHelpers.ContainsIgnoreCaseAccents((leading ?? "") + kv.Value + (trailing ?? ""), query));
    }


    /// <summary>
    /// Computes headwords for all writing systems present in CitationForm or LexemeForm,
    /// applying morph tokens when CitationForm is absent.
    /// Used for in-memory population of Entry.Headword after loading from DB.
    /// </summary>
    public static MultiString ComputeHeadwords(Entry entry,
        IReadOnlyDictionary<MorphTypeKind, MorphType> morphTypeDataLookup)
    {
        var result = new MultiString();
        morphTypeDataLookup.TryGetValue(entry.MorphType, out var morphData);

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
