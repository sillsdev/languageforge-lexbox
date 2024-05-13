using MongoDB.Driver;

namespace LfClassicData;

public static class MongoExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IMongoCollection<T> collection)
    {
        using var entriesCursor = await collection.Find(FilterDefinition<T>.Empty).ToCursorAsync();
        while (await entriesCursor.MoveNextAsync())
        {
            foreach (var entry in entriesCursor.Current)
            {
                yield return entry;
            }
        }
    }
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IAsyncCursorSource<T> cursorSource)
    {
        using var entriesCursor = await cursorSource.ToCursorAsync();
        while (await entriesCursor.MoveNextAsync())
        {
            foreach (var entry in entriesCursor.Current)
            {
                yield return entry;
            }
        }
    }
}
