using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;
using SIL.LCModel.Infrastructure;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class QueryEntryTests(ProjectLoaderFixture fixture) : QueryEntryTestsBase
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var entry = await Api.CreateEntry(new Entry { LexemeForm = new MultiString { { "en", "test" } } });

        var fwApi = (FwDataMiniLcmApi)BaseApi;
        var lexEntry = fwApi.EntriesRepository.GetObject(entry.Id);
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Set LexemeFormOA to null",
            "Restore LexemeFormOA",
            fwApi.Cache.ServiceLocator.ActionHandler,
            () =>
            {
                lexEntry.LexemeFormOA = null;
            });
    }

    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("query-entry-test", "en", "en"));
    }
}

[Collection(ProjectLoaderFixture.Name)]
public class NullAndEmptyQueryEntryTests(ProjectLoaderFixture fixture) : NullAndEmptyQueryEntryTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("null-and-empty-query-entry-test", "en", "en"));
    }
}
