using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;
using SIL.Harmony.Core;

namespace LexBoxApi.GraphQL.CustomTypes;

public class IsHarmonyProjectDataLoader(
    LexBoxDbContext dbContext,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<Guid, bool>(batchScheduler, options), IIsHarmonyProjectDataLoader
{
    protected override async Task<IReadOnlyDictionary<Guid, bool>> LoadBatchAsync(IReadOnlyList<Guid> keys, CancellationToken cancellationToken)
    {
        var isHarmonyProject = await dbContext.Set<ServerCommit>()
            .Where(c => keys.Contains(c.ProjectId))
            .Select(c => c.ProjectId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        return keys.ToDictionary(k => k, k => isHarmonyProject.Contains(k));
    }
}
