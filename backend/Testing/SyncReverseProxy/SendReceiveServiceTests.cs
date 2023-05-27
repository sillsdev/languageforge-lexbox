using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testing.Fixtures;

namespace Testing.Services;

public class SendReceiveServiceTests : IClassFixture<TestingServicesFixture>
{
    private SendReceiveService _srService;

    public SendReceiveServiceTests(TestingServicesFixture testing)
    {
        testing.Services.AddScoped<SendReceiveService>();
        _srService = testing.ServiceProvider.GetRequiredService<SendReceiveService>();
    }

    [Theory]
    [InlineData("3.0.1")]
    public async Task VerifyHgVersion(string expected)
    {
        string version = await _srService.VerifyHgVersion();
        version.ShouldContain(expected);
    }
}
