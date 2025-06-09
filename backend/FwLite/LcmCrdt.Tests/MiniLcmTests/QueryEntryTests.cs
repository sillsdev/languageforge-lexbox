using System.Diagnostics;
using Bogus;
using LcmCrdt.FullTextSearch;
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
        var faker = new Faker { Random = new Randomizer(8675309) };
        await _fixture.Api.BulkCreateEntries(AsyncEnumerable.Range(0, count).Select(i => new Entry { LexemeForm = { ["en"] = faker.Name.FirstName() } }));
        var entrySearchService = _fixture.GetService<EntrySearchService>();
        await entrySearchService
            .UpdateEntrySearchTable(await _fixture.Api.GetEntries(new QueryOptions(Count: -1)).ToArrayAsync());
        entrySearchService.EntrySearchRecords.Should().NotBeEmpty();
        outputHelper.WriteLine("Entries created");

        var testIterations = 10;
        var startTimestamp = Stopwatch.GetTimestamp();
        for (int i = 0; i < testIterations; i++)
        {
            //search should not match anything as we only want to test the match performance
            var results = await Api.SearchEntries("tes").ToArrayAsync();
            if (count == 50_000)
            {
                results.Should().HaveCount(39);
            }
            else
            {
                results.Should().HaveCount(74);
            }
        }

        var totalRuntime = Stopwatch.GetElapsedTime(startTimestamp);
        var queryTime = totalRuntime / testIterations;
        var timePerEntry = queryTime / count;
        outputHelper.WriteLine(
            $"Total query time: {queryTime.TotalMilliseconds}ms, time per entry: {timePerEntry.TotalMicroseconds}microseconds");
        timePerEntry.TotalMicroseconds.Should().BeLessThan(1);//on my machine I got 3.9, so this is a safe margin
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }
}
