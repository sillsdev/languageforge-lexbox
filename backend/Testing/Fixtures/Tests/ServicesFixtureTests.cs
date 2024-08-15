using Shouldly;

namespace Testing.Fixtures.Tests;

public class ServicesFixtureTests
{
    [Fact]
    public async Task CanSetupServices()
    {
        var fixture = new TestingServicesFixture();
        var act = async () =>
        {
            await fixture.InitializeAsync();
            await fixture.DisposeAsync();
        };
        Should.CompleteIn(act, TimeSpan.FromSeconds(10));
    }
}
