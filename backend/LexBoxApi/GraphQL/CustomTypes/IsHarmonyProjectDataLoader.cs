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
        var serverCommits = dbContext.Set<ServerCommit>().AsQueryable();
        if (keys.Count < 100) serverCommits = serverCommits.Where(c => keys.Contains(c.ProjectId));
        var isHarmonyProject = await serverCommits
            .Select(c => c.ProjectId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        if (keys.Count >= 100 && isHarmonyProject.Length >= 100)
        {
            // N is large enough that .Contains() being O(N) vs O(1) will matter
            var projectIds = isHarmonyProject.ToHashSet();
            return keys.ToDictionary(k => k, k => projectIds.Contains(k));
        }
        return keys.ToDictionary(k => k, k => isHarmonyProject.Contains(k));
    }
}
