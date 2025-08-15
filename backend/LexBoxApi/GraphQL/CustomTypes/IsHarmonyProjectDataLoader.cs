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
            .Select(c => c.ProjectId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        if (isHarmonyProject.Length > 100)
        {
            // N is large enough that .Contains() being O(N) vs O(1) will matter
            var projectIds = isHarmonyProject.ToHashSet();
            return keys.ToDictionary(k => k, k => projectIds.Contains(k));
        }
        return keys.ToDictionary(k => k, k => isHarmonyProject.Contains(k));
    }
}
