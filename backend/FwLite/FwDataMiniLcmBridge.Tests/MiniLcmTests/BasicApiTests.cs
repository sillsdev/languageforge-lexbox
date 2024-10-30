using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class BasicApiTests(ProjectLoaderFixture fixture) : BasicApiTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("create-entry-test", "en", "en"));
    }

    [Fact]
    public async Task UpdateEntry_WillInvalidateAndPreventMultipleUpdates()
    {
        var original = await Api.GetEntry(Entry1Id);
        await Task.Delay(1000);
        ArgumentNullException.ThrowIfNull(original);
        var update1 = (Entry)original.Copy();
        var update2 = (Entry)original.Copy();

        update1.LexemeForm["en"] = "updated";
        var updatedEntry = await Api.UpdateEntry(update1);
        updatedEntry.LexemeForm["en"].Should().Be("updated");
        updatedEntry.Should().BeEquivalentTo(update1, options => options.Excluding(e => e.Version));


        update2.LexemeForm["es"] = "updated again";
        Func<Task> action = async () => await Api.UpdateEntry(update2);
        await action.Should().ThrowAsync<VersionInvalidException>();
    }
}
