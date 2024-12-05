using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;

namespace FwLiteProjectSync.Tests;

public class ReorderTests : IClassFixture<SyncFixture>
{
    private static readonly AutoFaker AutoFaker = new(builder => builder.WithOverride(new MultiStringOverride()).WithOverride(new ObjectWithIdOverride()));
    public ReorderTests(SyncFixture fixture)
    {
        _fixture = fixture;
    }

    private readonly SyncFixture _fixture;


    [Fact]
    public async Task CanReorderSensesViaSync()
    {
        var entry = await _fixture.CrdtApi.CreateEntry(new() { LexemeForm = { { "en", "complexForm1" } } });
        var sense1 = await _fixture.CrdtApi.CreateSense(entry.Id, new Sense()
        {
            Gloss = { { "en", "1" } },
        });
        var sense2 = await _fixture.CrdtApi.CreateSense(entry.Id, new Sense()
        {
            Gloss = { { "en", "2" } },
        });
        var sense3 = await _fixture.CrdtApi.CreateSense(entry.Id, new Sense()
        {
            Gloss = { { "en", "3" } },
        });

        var before = (await _fixture.CrdtApi.GetEntry(entry.Id))!;
        before.Senses.Should().BeEquivalentTo([sense1, sense2, sense3], options => options.WithStrictOrdering());
        before.Senses.Select(s => s.Order).Should().BeEquivalentTo([1, 2, 3]);

        var after = before!.Copy();
        after.Senses = [sense3, sense2, sense1];

        await EntrySync.Sync(before, after, _fixture.CrdtApi);

        var actual = await _fixture.CrdtApi.GetEntry(after.Id);
        actual.Should().NotBeNull();
        for (var i = 0; i < after.Senses.Count; i++)
        {
            actual!.Senses[i].Should().BeEquivalentTo(after.Senses[i], options => options.Excluding(x => x.Order));
        }
    }
}
