using FwLiteProjectSync.Tests.Fixtures;

namespace FwLiteProjectSync.Tests;

public class SyncFixtureTests
{
    [Fact]
    public async Task CanStart()
    {
        var fixture = new SyncFixture();
        await fixture.InitializeAsync();
        await fixture.DisposeAsync();
    }
}
