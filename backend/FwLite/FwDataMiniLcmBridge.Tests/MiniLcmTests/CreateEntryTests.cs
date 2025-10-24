using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class CreateEntryTests(ProjectLoaderFixture fixture) : CreateEntryTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("create-entry-test", "en", "en"));
    }

    [Fact]
    public async Task CreateEntry_SetsMorphType()
    {
        var entry = await Api.CreateEntry(new Entry() { LexemeForm = { ["en"] = "test" } });
        var fwDataApi = (FwDataMiniLcmApi)BaseApi;
        var lexEntry = fwDataApi.EntriesRepository.GetObject(entry.Id);
        lexEntry.Should().NotBeNull();
        lexEntry.LexemeFormOA.MorphTypeRA.Should().NotBeNull();
        lexEntry.LexemeFormOA.MorphTypeRA.Guid.Should().Be(MoMorphTypeTags.kguidMorphStem);
        lexEntry.LexemeFormOA.MorphTypeRA.Name.UiString.Should().Be("stem");

        lexEntry.PrimaryMorphType.Should().NotBeNull();
        lexEntry.PrimaryMorphType.Guid.Should().Be(MoMorphTypeTags.kguidMorphStem);
        lexEntry.PrimaryMorphType.Name.UiString.Should().Be("stem");
    }
}
