using MiniLcm.Models;

namespace MiniLcm;

public static class MiniLcmExtensions
{
    public static IAsyncEnumerable<Entry> GetAllEntries(this IMiniLcmReadApi api)
    {
        return api.GetEntries(new QueryOptions(Count: QueryOptions.QueryAll));
    }

    public static async Task<ProjectSnapshot> TakeProjectSnapshot(this IMiniLcmReadApi api)
    {
        return new ProjectSnapshot(
            await api.GetAllEntries().ToArrayAsync(),
            await api.GetPartsOfSpeech().ToArrayAsync(),
            await api.GetPublications().ToArrayAsync(),
            await api.GetSemanticDomains().ToArrayAsync(),
            await api.GetComplexFormTypes().ToArrayAsync(),
            await api.GetWritingSystems());
    }
}
