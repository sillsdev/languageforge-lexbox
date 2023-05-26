using Shouldly;

namespace Testing.Services;

public class SendReceiveServiceTests
{
    private SendReceiveService _srService;

    public SendReceiveServiceTests(SendReceiveService srService)
    {
        _srService = srService;
    }

    [Theory]
    [InlineData("3.0.1")]
    public async Task VerifyHgVersion(string expected)
    {
        string version = await _srService.VerifyHgVersion();
        version.ShouldContain(expected);
    }
}
