using LexBoxApi.GraphQL.CustomTypes;
using Microsoft.Extensions.Time.Testing;
using Polly;
using Shouldly;

namespace Testing.Services;

public class IsLanguageForgeProjectDataLoaderTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly ResiliencePipeline<IReadOnlyDictionary<string, bool>> _pipeline;
    private static readonly TimeSpan BreakDuration = TimeSpan.FromSeconds(60);

    public IsLanguageForgeProjectDataLoaderTests()
    {
        _pipeline = IsLanguageForgeProjectDataLoader.ConfigureResiliencePipeline(new() { TimeProvider = _timeProvider }, BreakDuration)
            .Build();
    }

    private ValueTask<Outcome<Dictionary<string, bool>>> Execute(Exception? exception = null)
    {
        ResilienceContext context = ResilienceContextPool.Shared.Get();
        context.Properties.Set(IsLanguageForgeProjectDataLoader.ProjectCodesKey, new[] { "test" });
        return _pipeline.ExecuteOutcomeAsync((context, state) =>
            {
                if (exception is not null)
                {
                    return Outcome.FromExceptionAsValueTask<Dictionary<string, bool>>(exception);
                }

                return Outcome.FromResultAsValueTask(new Dictionary<string, bool>() { { "test", true } });
            },
            context,
            this);
    }

    private void VerifyEmptyResult(Outcome<Dictionary<string, bool>> result)
    {
        result.Exception.ShouldBeNull();
        result.Result.ShouldBe(new Dictionary<string, bool>() { { "test", false } });
    }

    private void VerifySuccessResult(Outcome<Dictionary<string, bool>> result)
    {
        result.Exception.ShouldBeNull();
        result.Result.ShouldBe(new Dictionary<string, bool>() { { "test", true } });
    }

    [Fact]
    public async Task ResiliencePipelineWorksFine()
    {
        var result = await Execute();
        VerifySuccessResult(result);
    }

    [Fact]
    public async Task ResiliencePipelineReturnsEmptyResultWhenExceptionIsThrown()
    {
        var result = await Execute(new Exception("test"));
        VerifyEmptyResult(result);
    }

    [Fact]
    public async Task CircuitBreaksAfter2Failures()
    {
        for (int i = 0; i < 3; i++)
        {
            await Execute(new Exception("test"));
            _timeProvider.Advance(TimeSpan.FromSeconds(21));
        }
        //the circuit is open, now the fallback should be used
        var result = await Execute();
        VerifyEmptyResult(result);
    }

    [Fact]
    public async Task CircuitBreaksAndReOpensAfterTimeout()
    {
        for (int i = 0; i < 3; i++)
        {
            await Execute(new Exception("test"));
            _timeProvider.Advance(TimeSpan.FromSeconds(21));
        }
        //the circuit is open, now the fallback should be used
        VerifyEmptyResult(await Execute());
        _timeProvider.Advance(BreakDuration + TimeSpan.FromSeconds(1));
        VerifySuccessResult(await Execute());
    }
}
