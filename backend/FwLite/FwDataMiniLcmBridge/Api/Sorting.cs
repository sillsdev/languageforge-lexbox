using MiniLcm;
using MiniLcm.Culture;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api;

internal static class Sorting
{
    public static IEnumerable<ILexEntry> ApplyHeadwordOrder(this IEnumerable<ILexEntry> entries, SortOptions order, int sortWsHandle, int stemSecondaryOrder)
    {
        if (order.Ascending)
        {
            return entries
                .OrderBy(e => e.LexEntryHeadword(sortWsHandle, applyMorphTokens: false))
                .ThenBy(e => e.PrimaryMorphType?.SecondaryOrder ?? stemSecondaryOrder)
                // .ThenBy(e => e.HomographNumber)
                .ThenBy(e => e.Id.Guid);
        }
        else
        {
            return entries
                .OrderByDescending(e => e.LexEntryHeadword(sortWsHandle, applyMorphTokens: false))
                .ThenByDescending(e => e.PrimaryMorphType?.SecondaryOrder ?? stemSecondaryOrder)
                // .ThenByDescending(e => e.HomographNumber)
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
                // .ThenBy(x => x.Entry.HomographNumber)
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
                // .ThenByDescending(x => x.Entry.HomographNumber)
                .ThenByDescending(x => x.Entry.Id.Guid)
                .Select(x => x.Entry);
        }
    }
}
