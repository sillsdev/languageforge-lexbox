using System.Diagnostics;
using Xunit.Abstractions;

namespace LcmCrdt.Tests.MiniLcmTests;

public class QueryEntryTests(ITestOutputHelper outputHelper) : QueryEntryTestsBase
{
    private readonly MiniLcmApiFixture _fixture = new();

    protected override async Task<IMiniLcmApi> NewApi()
    {
        await _fixture.InitializeAsync();
        var api = _fixture.Api;
        return api;
    }



    [Theory]
    [InlineData(50_000)]
    [InlineData(100_000)]
    public async Task QueryPerformanceTesting(int count)
    {
        await _fixture.Api.BulkCreateEntries(AsyncEnumerable.Range(0, count).Select(i => new Entry { LexemeForm = { ["en"] = Guid.NewGuid().ToString() } }));

        var testIterations = 10;
        var startTimestamp = Stopwatch.GetTimestamp();
        for (int i = 0; i < testIterations; i++)
        {
            //search should not match anything as we only want to test the match performance
            var results = await Api.SearchEntries("asdfgbope").ToArrayAsync();
            results.Should().BeEmpty();
        }

        var totalRuntime = Stopwatch.GetElapsedTime(startTimestamp);
        var queryTime = totalRuntime / testIterations;
        var timePerEntry = queryTime / count;
        outputHelper.WriteLine(
            $"Total query time: {queryTime.TotalMilliseconds}ms, time per entry: {timePerEntry.TotalMicroseconds}microseconds");
        timePerEntry.TotalMicroseconds.Should().BeLessThan(15);//on my machine I got 3.9, increased the margin as CI was failing at 10
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }
}
