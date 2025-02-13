using MiniLcm;
using MiniLcm.Models;

namespace FwLiteProjectSync;

public static class MiniLcmExtensions
{
    public static IAsyncEnumerable<Entry> GetAllEntries(this IMiniLcmApi api)
    {
        return api.GetEntries(new QueryOptions(Count: QueryOptions.QueryAll));
    }
}
