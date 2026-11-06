using FwHeadless.Services;

namespace Testing.FwHeadless;

public class HarmonySyncSchedulerTests
{
    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(5, 160)]
    [InlineData(6, 300)]
    [InlineData(1000, 300)]
    public void DelayIsTheDebounceThenBacksOffToFiveMinutes(int consecutiveFailures, int expectedSeconds)
    {
        HarmonySyncScheduler.Delay(consecutiveFailures).Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }
}
