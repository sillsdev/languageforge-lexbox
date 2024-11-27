using FluentAssertions;

namespace Testing.Fixtures.Tests;

public class ServicesFixtureTests
{
    [Fact]
    public async Task CanSetupServices()
    {
        var fixture = TestingServicesFixture.Create("lexbox-service-fixture-test");
        var act = async () =>
        {
            await fixture.InitializeAsync();
            await fixture.DisposeAsync();
        };
        await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(10));
    }
}
