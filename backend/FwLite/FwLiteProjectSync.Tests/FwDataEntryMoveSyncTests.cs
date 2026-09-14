using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm;

namespace FwLiteProjectSync.Tests;

public class FwDataEntryMoveSyncTests(ExtraWritingSystemsSyncFixture fixture) : EntryMoveSyncTestsBase(fixture)
{
    protected override IMiniLcmApi GetApi(SyncFixture fixture)
    {
        return fixture.FwDataApi;
    }
}
