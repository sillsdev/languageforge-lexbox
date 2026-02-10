using MiniLcm;
using MiniLcm.Culture;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api;

internal static class Sorting
{
    /// <summary>
    /// Rough emulation of FTS search relevance. Headword matches come first, preferring
    /// prefix matches (e.g. when searching "tan" then "tanan" is before "matan"), then shorter, then alphabetical.
    /// See also: EntrySearchService.FilterAndRank for the FTS-based equivalent in LcmCrdt.
    /// </summary>
    public static IEnumerable<ILexEntry> ApplyRoughBestMatchOrder(this IEnumerable<ILexEntry> entries, SortOptions order, int sortWsHandle, string? query = null)
    {
        var projected = entries.Select(e => (Entry: e, Headword: e.LexEntryHeadword(sortWsHandle)));
        if (order.Ascending)
        {
            return projected
                .OrderByDescending(x => !string.IsNullOrEmpty(query) && (x.Headword?.ContainsDiacriticMatch(query!) ?? false))
                .ThenByDescending(x => !string.IsNullOrEmpty(query) && (x.Headword?.StartsWithDiacriticMatch(query!) ?? false))
                .ThenBy(x => x.Headword?.Length ?? 0)
                .ThenBy(x => x.Headword)
                .ThenBy(x => x.Entry.Id.Guid)
                .Select(x => x.Entry);
        }
        else
        {
            return projected
                .OrderBy(x => !string.IsNullOrEmpty(query) && (x.Headword?.ContainsDiacriticMatch(query!) ?? false))
                .ThenBy(x => !string.IsNullOrEmpty(query) && (x.Headword?.StartsWithDiacriticMatch(query!) ?? false))
                .ThenByDescending(x => x.Headword?.Length ?? 0)
                .ThenByDescending(x => x.Headword)
                .ThenByDescending(x => x.Entry.Id.Guid)
                .Select(x => x.Entry);
        }
    }
}
