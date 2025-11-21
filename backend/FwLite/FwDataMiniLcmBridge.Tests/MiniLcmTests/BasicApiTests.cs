using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;
using SIL.LCModel.Infrastructure;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class BasicApiTests(ProjectLoaderFixture fixture) : BasicApiTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("create-entry-test", "en", "en"));
    }

    [Fact]
    public async Task GetEntry_WithNullLexemeFormOA_CreatesNewLexemeForm()
    {
        // Arrange
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString { { "en", "test" } }
        });

        var fwApi = (FwDataMiniLcmApi)BaseApi;
        var lexEntry = fwApi.EntriesRepository.GetObject(entry.Id);
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Set LexemeFormOA to null",
            "Restore LexemeFormOA",
            fwApi.Cache.ServiceLocator.ActionHandler,
            () =>
            {
                lexEntry.LexemeFormOA = null;
            });

        // Act
        var retrievedEntry = await Api.GetEntry(entry.Id);

        // Assert
        retrievedEntry.Should().NotBeNull();
        retrievedEntry.LexemeForm.Should().NotBeNull();
        retrievedEntry.LexemeForm.Values.Should().BeEmpty(); // Since we deleted the LexemeFormOA, it should return an empty MultiString

        //ensure we can still get the entry
        (await Api.GetEntries().ToArrayAsync()).Should().NotBeEmpty();
    }
}
