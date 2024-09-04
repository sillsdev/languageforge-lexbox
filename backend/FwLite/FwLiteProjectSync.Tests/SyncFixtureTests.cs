using FwLiteProjectSync.Tests.Fixtures;

namespace FwLiteProjectSync.Tests;

public class SyncFixtureTests
{
    [Fact]
    public async Task CanStart()
    {
        var fixture = SyncFixture.Create("test-sync-fixture");
        await fixture.InitializeAsync();
        await fixture.DisposeAsync();
    }
}
