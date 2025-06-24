using MiniLcm;
using MiniLcm.Culture;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api;

internal static class Sorting
{
    /// <summary>
    /// crude emulation of FTS search relevance
    /// </summary>
    public static IEnumerable<ILexEntry> ApplyRoughBestMatchOrder(this IEnumerable<ILexEntry> entries, SortOptions order, int sortWsHandle, string? query = null)
    {
        if (order.Ascending)
        {
            return entries
                .OrderByDescending(e => !string.IsNullOrEmpty(query) && (e.LexEntryHeadword(sortWsHandle)?.ContainsDiacriticMatch(query!) ?? false))
                .ThenBy(e => e.LexEntryHeadword(sortWsHandle)?.Length ?? 0)
                .ThenBy(e => e.LexEntryHeadword(sortWsHandle))
                .ThenBy(e => e.Id.Guid);
        }
        else
        {
            return entries
                .OrderBy(e => !string.IsNullOrEmpty(query) && (e.LexEntryHeadword(sortWsHandle)?.ContainsDiacriticMatch(query!) ?? false))
                .ThenByDescending(e => e.LexEntryHeadword(sortWsHandle)?.Length ?? 0)
                .ThenByDescending(e => e.LexEntryHeadword(sortWsHandle))
                .ThenByDescending(e => e.Id.Guid);
        }
    }
}
