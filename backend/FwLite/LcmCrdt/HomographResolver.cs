using LcmCrdt.Data;
using LinqToDB.EntityFrameworkCore;

namespace LcmCrdt;

internal record HomographResolution(int NewEntryNumber, HomographPromotion? Promotion);
internal record HomographPromotion(Guid EntryId, int NewNumber);

internal static class HomographResolver
{
    /// <summary>
    /// Resolves the homograph number for a new entry by finding existing entries in the
    /// same "homograph scope" (same headword + SecondaryOrder) and assigning a number one higher than the max.
    /// Also looks for an existing "lone" entry that needs promoting to 1.
    /// </summary>
    internal static async Task<HomographResolution> ResolveForNewEntry(Entry entry, MiniLcmRepository repo)
    {
        var defaultVernacularWs = await repo.GetWritingSystem(default, WritingSystemType.Vernacular);
        if (defaultVernacularWs is null) return new HomographResolution(0, null);

        var wsId = defaultVernacularWs.WsId;
        var newHeadword = entry.Headword(wsId);
        if (string.IsNullOrEmpty(newHeadword)) return new HomographResolution(0, null);

        var morphTypes = repo.MorphTypes.ToLinqToDB();
        var stemOrder = morphTypes.Where(m => m.Kind == MorphTypeKind.Stem)
            .Select(m => m.SecondaryOrder);

        var secondaryOrder = morphTypes.Where(m => m.Kind == entry.MorphType)
            .Select(m => (int?)m.SecondaryOrder).FirstOrDefault()
            ?? stemOrder.FirstOrDefault();

        var matchingEntries = await (
            from e in repo.Entries
            // a simple "==" comparison matches LibLCM
            where e.Id != entry.Id && e.Headword(wsId) == newHeadword
            let eSo = morphTypes.Where(m => m.Kind == e.MorphType)
                .Select(m => (int?)m.SecondaryOrder).FirstOrDefault()
                ?? stemOrder.FirstOrDefault()
            where eSo == secondaryOrder
            select new { e.Id, e.HomographNumber }
        ).ToListAsyncLinqToDB();

        if (matchingEntries.Count == 0) return new HomographResolution(0, null);

        // If there's exactly one existing entry with HomographNumber 0, it needs to be promoted to 1
        if (matchingEntries.Count == 1 && matchingEntries[0].HomographNumber == 0)
        {
            return new HomographResolution(2, new HomographPromotion(matchingEntries[0].Id, 1));
        }

        var maxHomograph = matchingEntries.Max(e => e.HomographNumber);
        return new HomographResolution(maxHomograph + 1, null);
    }
}
