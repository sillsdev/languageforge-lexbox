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
        if (order.Ascending)
        {
            return entries
                .OrderByDescending(e => !string.IsNullOrEmpty(query) && (e.LexEntryHeadword(sortWsHandle)?.ContainsDiacriticMatch(query!) ?? false))
                .ThenByDescending(e => !string.IsNullOrEmpty(query) && (e.LexEntryHeadword(sortWsHandle)?.StartsWithDiacriticMatch(query!) ?? false))
                .ThenBy(e => e.LexEntryHeadword(sortWsHandle)?.Length ?? 0)
                .ThenBy(e => e.LexEntryHeadword(sortWsHandle))
                .ThenBy(e => e.Id.Guid);
        }
        else
        {
            return entries
                .OrderBy(e => !string.IsNullOrEmpty(query) && (e.LexEntryHeadword(sortWsHandle)?.ContainsDiacriticMatch(query!) ?? false))
                .ThenBy(e => !string.IsNullOrEmpty(query) && (e.LexEntryHeadword(sortWsHandle)?.StartsWithDiacriticMatch(query!) ?? false))
                .ThenByDescending(e => e.LexEntryHeadword(sortWsHandle)?.Length ?? 0)
                .ThenByDescending(e => e.LexEntryHeadword(sortWsHandle))
                .ThenByDescending(e => e.Id.Guid);
        }
    }
}
