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

    /// <summary>
    /// Computes headwords for all vernacular writing systems, applying morph tokens when CitationForm is absent.
    /// Used for in-memory population of Entry.Headword after loading from DB.
    /// </summary>
    public static MultiString ComputeHeadwords(Entry entry,
        IReadOnlyDictionary<MorphType, MorphTypeData> morphTypeDataLookup,
        IEnumerable<WritingSystem> vernacularWritingSystems)
    {
        var result = new MultiString();
        morphTypeDataLookup.TryGetValue(entry.MorphType, out var morphData);

        foreach (var ws in vernacularWritingSystems)
        {
            var citation = entry.CitationForm[ws.WsId];
            if (!string.IsNullOrEmpty(citation))
            {
                result[ws.WsId] = citation.Trim();
                continue;
            }

            var lexeme = entry.LexemeForm[ws.WsId];
            if (!string.IsNullOrEmpty(lexeme))
            {
                var leading = morphData?.LeadingToken ?? "";
                var trailing = morphData?.TrailingToken ?? "";
                result[ws.WsId] = (leading + lexeme + trailing).Trim();
            }
        }

        return result;
    }
}
