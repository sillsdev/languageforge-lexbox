using FwHeadless;
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

    [Theory]
    [InlineData(1, 10, 10)]
    [InlineData(9, 10, 300)]
    [InlineData(1, 2, 10)]
    public void RetryDelayBacksOffBelowTheFailureLimit(int consecutiveFailures, int maxConsecutiveFailures, int expectedSeconds)
    {
        HarmonySyncScheduler.RetryDelay(consecutiveFailures, maxConsecutiveFailures)
            .Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(11, 10)]
    [InlineData(1, 1)]
    public void RetryDelayGivesUpAtTheFailureLimit(int consecutiveFailures, int maxConsecutiveFailures)
    {
        HarmonySyncScheduler.RetryDelay(consecutiveFailures, maxConsecutiveFailures).Should().BeNull();
    }

    [Fact]
    public void TheDefaultFailureLimitRetriesForAboutTwentyFiveMinutes()
    {
        var config = new FwHeadlessConfig
        {
            LexboxUrl = "http://localhost/", LexboxUsername = "", LexboxPassword = "",
            ProjectStorageRoot = "", MediaFileAuthority = ""
        };
        var totalWait = Enumerable.Range(1, config.HarmonySyncMaxConsecutiveFailures - 1)
            .Aggregate(TimeSpan.Zero, (sum, failures) => sum + HarmonySyncScheduler.Delay(failures));
        totalWait.Should().Be(TimeSpan.FromSeconds(1510));
    }
}
