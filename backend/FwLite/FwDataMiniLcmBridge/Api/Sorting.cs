using MiniLcm;
using MiniLcm.Culture;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api;

internal static class Sorting
{
    /// <summary>
    /// Expands entries to one row per sense (an entry without senses keeps a single row) ordered by gloss.
    /// Rows without a gloss in the sort writing system always sort last and tiebreakers always ascend,
    /// so only the gloss order itself flips with <see cref="SortOptions.Ascending"/>.
    /// Mirrors <c>LcmCrdt.Data.Sorting.ApplyGlossOrder</c>.
    /// </summary>
    public static IEnumerable<(ILexEntry Entry, ILexSense? Sense)> ApplyGlossOrder(
        this IEnumerable<ILexEntry> entries,
        SortOptions order,
        int glossWsHandle,
        int headwordWsHandle,
        int stemSecondaryOrder)
    {
        var projected = entries.SelectMany(e =>
            e.AllSenses.Select((s, i) => (Entry: e, Sense: (ILexSense?)s, SenseIndex: i))
                .DefaultIfEmpty((Entry: e, Sense: null, SenseIndex: -1)))
            .Select(x => (x.Entry, x.Sense, x.SenseIndex, Gloss: x.Sense?.Gloss.get_String(glossWsHandle).Text));

        var withoutGlossLast = projected.OrderBy(x => string.IsNullOrEmpty(x.Gloss));
        var byGloss = order.Ascending
            ? withoutGlossLast.ThenBy(x => x.Gloss)
            : withoutGlossLast.ThenByDescending(x => x.Gloss);
        return byGloss
            .ThenBy(x => x.Entry.LexEntryHeadword(headwordWsHandle, applyMorphTokens: false))
            .ThenBy(x => x.Entry.PrimaryMorphType?.SecondaryOrder ?? stemSecondaryOrder)
            .ThenBy(x => x.Entry.HomographNumber)
            .ThenBy(x => x.Entry.Id.Guid)
            .ThenBy(x => x.SenseIndex)
            .Select(x => (x.Entry, x.Sense));
    }

    public static IEnumerable<ILexEntry> ApplyHeadwordOrder(this IEnumerable<ILexEntry> entries, SortOptions order, int sortWsHandle, int stemSecondaryOrder)
    {
        if (order.Ascending)
        {
            return entries
                .OrderBy(e => e.LexEntryHeadword(sortWsHandle, applyMorphTokens: false))
                .ThenBy(e => e.PrimaryMorphType?.SecondaryOrder ?? stemSecondaryOrder)
                .ThenBy(e => e.HomographNumber)
                .ThenBy(e => e.Id.Guid);
        }
        else
        {
            return entries
                .OrderByDescending(e => e.LexEntryHeadword(sortWsHandle, applyMorphTokens: false))
                .ThenByDescending(e => e.PrimaryMorphType?.SecondaryOrder ?? stemSecondaryOrder)
                .ThenByDescending(e => e.HomographNumber)
                .ThenByDescending(e => e.Id.Guid);
        }
    }

    /// <summary>
    /// Rough emulation of FTS search relevance. Headword matches come first, preferring
    /// prefix matches (e.g. when searching "tan" then "tanan" is before "matan"), then shorter, then alphabetical.
    /// See also: EntrySearchService.FilterAndRank for the FTS-based equivalent in LcmCrdt.
    /// </summary>
    public static IEnumerable<ILexEntry> ApplyRoughBestMatchOrder(this IEnumerable<ILexEntry> entries, SortOptions order, int sortWsHandle, int stemSecondaryOrder, string? query = null)
    {
        var projected = entries.Select(e => (
            Entry: e,
            Headword: e.LexEntryHeadword(sortWsHandle, applyMorphTokens: false),
            HeadwordWithTokens: e.LexEntryHeadword(sortWsHandle, applyMorphTokens: true)
        ));
        if (order.Ascending)
        {
            return projected
                .OrderByDescending(x => !string.IsNullOrEmpty(query) && (x.HeadwordWithTokens?.StartsWithDiacriticMatch(query!) ?? false))
                .ThenByDescending(x => !string.IsNullOrEmpty(query) && (x.HeadwordWithTokens?.ContainsDiacriticMatch(query!) ?? false))
                .ThenBy(x => x.Headword?.Length ?? 0)
                .ThenBy(x => x.Headword)
                .ThenBy(x => x.Entry.PrimaryMorphType?.SecondaryOrder ?? stemSecondaryOrder)
                .ThenBy(x => x.Entry.HomographNumber)
                .ThenBy(x => x.Entry.Id.Guid)
                .Select(x => x.Entry);
        }
        else
        {
            return projected
                .OrderBy(x => !string.IsNullOrEmpty(query) && (x.HeadwordWithTokens?.StartsWithDiacriticMatch(query!) ?? false))
                .ThenBy(x => !string.IsNullOrEmpty(query) && (x.HeadwordWithTokens?.ContainsDiacriticMatch(query!) ?? false))
                .ThenByDescending(x => x.Headword?.Length ?? 0)
                .ThenByDescending(x => x.Headword)
                .ThenByDescending(x => x.Entry.PrimaryMorphType?.SecondaryOrder ?? stemSecondaryOrder)
                .ThenByDescending(x => x.Entry.HomographNumber)
                .ThenByDescending(x => x.Entry.Id.Guid)
                .Select(x => x.Entry);
        }
    }
}
