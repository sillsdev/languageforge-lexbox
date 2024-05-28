using LexCore.ServiceInterfaces;
using LfClassicData;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LexBoxApi.GraphQL.CustomTypes;

public class IsLanguageForgeProjectDataLoader : BatchDataLoader<string, bool>, IIsLanguageForgeProjectDataLoader
{
    private readonly SystemDbContext _systemDbContext;

    public IsLanguageForgeProjectDataLoader(
        SystemDbContext systemDbContext,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _systemDbContext = systemDbContext;
    }

    protected override async Task<IReadOnlyDictionary<string, bool>> LoadBatchAsync(
        IReadOnlyList<string> projectCodes,
        CancellationToken cancellationToken)
    {
        return await MongoExtensions.ToAsyncEnumerable(_systemDbContext.Projects.AsQueryable()
            .Select(p => p.ProjectCode)
            .Where(projectCode => projectCodes.Contains(projectCode)))
            .ToDictionaryAsync(projectCode => projectCode, _ => true, cancellationToken);
    }
}
