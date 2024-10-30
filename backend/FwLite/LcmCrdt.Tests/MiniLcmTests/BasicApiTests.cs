using Xunit.Abstractions;

namespace LcmCrdt.Tests.MiniLcmTests;

public class BasicApiTests(ITestOutputHelper output): BasicApiTestsBase
{
    private readonly MiniLcmApiFixture _fixture = new();
    public override async Task InitializeAsync()
    {
        _fixture.LogTo(output);
        await _fixture.InitializeAsync();
        await base.InitializeAsync();
    }

    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(_fixture.Api);
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task UpdateEntry_CanUseSameVersionMultipleTimes()
    {
        var original = await Api.GetEntry(Entry1Id);
        await Task.Delay(1000);
        ArgumentNullException.ThrowIfNull(original);
        var update1 = (Entry) original.Copy();
        var update2 = (Entry)original.Copy();

        update1.LexemeForm["en"] = "updated";
        var updatedEntry = await Api.UpdateEntry(update1);
        updatedEntry.LexemeForm["en"].Should().Be("updated");
        updatedEntry.Should().BeEquivalentTo(update1, options => options.Excluding(e => e.Version));


        update2.LexemeForm["es"] = "updated again";
        var updatedEntry2 = await Api.UpdateEntry(update2);
        updatedEntry2.LexemeForm["en"].Should().Be("updated");
        updatedEntry2.LexemeForm["es"].Should().Be("updated again");
        updatedEntry2.Should().BeEquivalentTo(update2, options => options.Excluding(e => e.Version).Excluding(e => e.LexemeForm));
    }
}
