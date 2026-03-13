using System.Linq.Expressions;
using LinqToDB;
using MiniLcm.Models;

namespace LcmCrdt.Data;

public static class EntryQueryHelpers
{
    [ExpressionMethod(nameof(HeadwordExpression))]
    public static string Headword(this Entry e, WritingSystemId ws)
    {
        var word = e.CitationForm[ws];
        if (string.IsNullOrEmpty(word)) word = e.LexemeForm[ws];
        return word.Trim();
    }

    private static Expression<Func<Entry, WritingSystemId, string?>> HeadwordExpression() =>
        (e, ws) => (string.IsNullOrEmpty(Json.Value(e.CitationForm, ms => ms[ws]))
            ? Json.Value(e.LexemeForm, ms => ms[ws])
            : Json.Value(e.CitationForm, ms => ms[ws]))!.Trim();

    [ExpressionMethod(nameof(HeadwordWithTokensExpression))]
    public static string HeadwordWithTokens(this Entry e, WritingSystemId ws, string? leading, string? trailing)
    {
        var citation = e.CitationForm[ws];
        if (!string.IsNullOrEmpty(citation)) return citation.Trim();
        var lexeme = e.LexemeForm[ws];
        if (string.IsNullOrEmpty(lexeme)) return string.Empty;
        return ((leading ?? "") + lexeme + (trailing ?? "")).Trim();
    }

    private static Expression<Func<Entry, WritingSystemId, string?, string?, string?>> HeadwordWithTokensExpression() =>
        (e, ws, leading, trailing) =>
            string.IsNullOrEmpty(Json.Value(e.CitationForm, ms => ms[ws]))
                ? string.IsNullOrEmpty(Json.Value(e.LexemeForm, ms => ms[ws]))
                    ? ""
                    : ((leading ?? "") + Json.Value(e.LexemeForm, ms => ms[ws]) + (trailing ?? "")).Trim()
                : Json.Value(e.CitationForm, ms => ms[ws])!.Trim();

    // === Morph-token-aware headword expressions ===
    //
    // HeadwordSearchValue checks headwords (with morph tokens) across ALL writing systems.
    // Use with a MorphTypeData JOIN to get the leading/trailing tokens:
    //
    //   from morphData in morphTypes.LeftJoin(m => (int)m.MorphType == (int)entry.MorphType)
    //   where entry.HeadwordSearchValue(morphData.LeadingToken, morphData.TrailingToken, query)
    //       || entry.Senses.Any(s => s.Gloss.SearchValue(query))
    //
    // For per-WS headword with tokens:
    //
    //   let headword = entry.HeadwordWithTokens(wsId, morphData.LeadingToken, morphData.TrailingToken)

    [ExpressionMethod(nameof(HeadwordSearchValueExpression))]
    public static bool HeadwordSearchValue(this Entry e, string? leading, string? trailing, string query)
    {
        return e.CitationForm.SearchValue(query)
            || e.LexemeForm.Values.Any(kvp =>
                SqlHelpers.ContainsIgnoreCaseAccents((leading ?? "") + kvp.Value + (trailing ?? ""), query));
    }

    private static Expression<Func<Entry, string?, string?, string, bool>> HeadwordSearchValueExpression() =>
        (e, leading, trailing, query) =>
            Json.QueryValues(e.CitationForm).Any(v => SqlHelpers.ContainsIgnoreCaseAccents(v, query))
            || Json.QueryValues(e.LexemeForm).Any(v =>
                SqlHelpers.ContainsIgnoreCaseAccents((leading ?? "") + v + (trailing ?? ""), query));

    /// <summary>
    /// Computes headwords for all writing systems present in CitationForm or LexemeForm,
    /// applying morph tokens when CitationForm is absent.
    /// Used for in-memory population of Entry.Headword after loading from DB.
    /// </summary>
    public static MultiString ComputeHeadwords(Entry entry,
        IReadOnlyDictionary<MorphType, MorphTypeData> morphTypeDataLookup)
    {
        var result = new MultiString();
        morphTypeDataLookup.TryGetValue(entry.MorphType, out var morphData);

        // Iterate all WS keys that have data, not just "current" vernacular WSs,
        // so we don't lose headwords for non-current or future writing systems.
        var wsIds = entry.CitationForm.Values.Keys
            .Union(entry.LexemeForm.Values.Keys);

        foreach (var wsId in wsIds)
        {
            var citation = entry.CitationForm[wsId];
            if (!string.IsNullOrEmpty(citation))
            {
                result[wsId] = citation.Trim();
                continue;
            }

            var lexeme = entry.LexemeForm[wsId];
            if (!string.IsNullOrEmpty(lexeme))
            {
                var leading = morphData?.LeadingToken ?? "";
                var trailing = morphData?.TrailingToken ?? "";
                result[wsId] = (leading + lexeme + trailing).Trim();
            }
        }

        return result;
    }
}
