using LexCore.ServiceInterfaces;
using LfClassicData;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Registry;

namespace LexBoxApi.GraphQL.CustomTypes;

public class IsLanguageForgeProjectDataLoader : BatchDataLoader<string, bool>, IIsLanguageForgeProjectDataLoader
{
    public const string ResiliencePolicyName = "IsLanguageForgeProjectDataLoader";
    private readonly SystemDbContext _systemDbContext;
    private readonly ResiliencePipeline<IReadOnlyDictionary<string, bool>> _resiliencePipeline;

    public IsLanguageForgeProjectDataLoader(
        SystemDbContext systemDbContext,
        IBatchScheduler batchScheduler,
        [FromKeyedServices(ResiliencePolicyName)]
        ResiliencePipeline<IReadOnlyDictionary<string, bool>> resiliencePipeline,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _resiliencePipeline = resiliencePipeline;
        _systemDbContext = systemDbContext;
    }

    protected override async Task<IReadOnlyDictionary<string, bool>> LoadBatchAsync(
        IReadOnlyList<string> projectCodes,
        CancellationToken cancellationToken)
    {
        return await FaultTolerantLoadBatch(projectCodes, cancellationToken);
    }

    private async ValueTask<IReadOnlyDictionary<string, bool>> FaultTolerantLoadBatch(
        IReadOnlyList<string> projectCodes,
        CancellationToken cancellationToken)
    {
        ResilienceContext context = ResilienceContextPool.Shared.Get(cancellationToken);
        context.Properties.Set(ProjectCodesKey, projectCodes);
        try
        {
            return await _resiliencePipeline.ExecuteAsync(
                static async (context, state) => await LoadBatch(state,
                    context.Properties.GetValue(ProjectCodesKey, []),
                    context.CancellationToken),
                context,
                this);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    private static async Task<Dictionary<string, bool>> LoadBatch(IsLanguageForgeProjectDataLoader loader,
        IReadOnlyList<string> list,
        CancellationToken token)
    {
        return await MongoExtensions.ToAsyncEnumerable(loader._systemDbContext.Projects.AsQueryable()
                .Select(p => p.ProjectCode)
                .Where(projectCode => list.Contains(projectCode)))
            .ToDictionaryAsync(projectCode => projectCode, _ => true, token);
    }


    public static readonly ResiliencePropertyKey<IReadOnlyList<string>> ProjectCodesKey = new("project-codes");

    public static ResiliencePipelineBuilder<IReadOnlyDictionary<string, bool>> ConfigureResiliencePipeline(
        ResiliencePipelineBuilder<IReadOnlyDictionary<string, bool>> builder, TimeSpan circuitBreakerDuration)
    {
        var circuitBreakerStrategyOptions = new CircuitBreakerStrategyOptions<IReadOnlyDictionary<string, bool>>
        {
            //docs https://www.pollydocs.org/strategies/circuit-breaker.html
            Name = "IsLanguageForgeProjectDataLoaderCircuitBreaker",
            MinimumThroughput = 2,//must be at least 2
            BreakDuration = circuitBreakerDuration,
            //window in which the minimum throughput can be reached.
            //ff there is only 1 failure in an hour, then the circuit will not break,
            //but the moment there is a second failure then it will break immediately.
            SamplingDuration = TimeSpan.FromHours(1),
        };
        var fallbackStrategyOptions = new FallbackStrategyOptions<IReadOnlyDictionary<string, bool>>()
        {
            //docs https://www.pollydocs.org/strategies/fallback.html
            Name = "IsLanguageForgeProjectDataLoaderFallback",
            FallbackAction = arguments =>
            {
                IReadOnlyDictionary<string, bool> emptyResult = arguments.Context.Properties
                    .GetValue(ProjectCodesKey, []).ToDictionary(pc => pc, _ => false);
                return Outcome.FromResultAsValueTask(emptyResult);
            }
        };
        builder
            .AddFallback(fallbackStrategyOptions)
            .AddCircuitBreaker(circuitBreakerStrategyOptions)
            ;
        return builder;
    }
}
