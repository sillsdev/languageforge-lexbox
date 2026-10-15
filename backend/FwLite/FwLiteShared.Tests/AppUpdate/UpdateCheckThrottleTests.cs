using System.Globalization;
using FwLiteShared.AppUpdate;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FwLiteShared.Tests.AppUpdate;

public class UpdateCheckThrottleTests
{
    private readonly InMemoryPreferencesService _preferences = new();

    private UpdateCheckThrottle CreateThrottle(FwLiteConfig? config = null) =>
        new(_preferences,
            Options.Create(config ?? new FwLiteConfig()),
            Mock.Of<ILogger<UpdateCheckThrottle>>());

    private void SetLastCheck(DateTime time) =>
        _preferences.Set(UpdateCheckThrottle.LastUpdateCheckKey, time.ToString("O", CultureInfo.InvariantCulture));

    [Fact]
    public void ShouldCheckForUpdate_WhenConfigSetToNever_ReturnsFalse()
    {
        var throttle = CreateThrottle(new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.Never });

        throttle.ShouldCheckForUpdate().Should().BeFalse();
    }

    [Fact]
    public void ShouldCheckForUpdate_WhenConfigSetToAlways_ReturnsTrue()
    {
        var throttle = CreateThrottle(new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.Always });

        throttle.ShouldCheckForUpdate().Should().BeTrue();
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(9, true)]
    [InlineData(24, true)]
    [InlineData(28, true)]
    public void ShouldCheckForUpdate_RespectsDefaultInterval(int hoursSinceLastCheck, bool expectedResult)
    {
        var throttle = CreateThrottle(new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.OnInterval });
        SetLastCheck(DateTime.UtcNow.AddHours(-hoursSinceLastCheck));

        throttle.ShouldCheckForUpdate().Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    public void ShouldCheckForUpdate_RespectsCustomInterval(int hoursSinceLastCheck, bool expectedResult)
    {
        var throttle = CreateThrottle(new FwLiteConfig
        {
            UpdateCheckCondition = UpdateCheckCondition.OnInterval,
            UpdateCheckInterval = TimeSpan.FromHours(4)
        });
        SetLastCheck(DateTime.UtcNow.AddHours(-hoursSinceLastCheck));

        throttle.ShouldCheckForUpdate().Should().Be(expectedResult);
    }

    [Fact]
    public void ShouldCheckForUpdate_WhenLastCheckInFuture_ReturnsTrue()
    {
        var throttle = CreateThrottle(new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.OnInterval });
        SetLastCheck(DateTime.UtcNow.AddHours(2));

        throttle.ShouldCheckForUpdate()
            .Should().BeTrue("because a future timestamp indicates clock skew and should trigger a check");
    }

    [Fact]
    public void ShouldCheckForUpdate_WhenNeverCheckedBefore_ReturnsTrue()
    {
        var throttle = CreateThrottle(new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.OnInterval });

        throttle.ShouldCheckForUpdate()
            .Should().BeTrue("because no stored last-check time means never checked");
    }

    [Fact]
    public void RecordCheck_PersistsNow_SoNextCheckIsThrottled()
    {
        var throttle = CreateThrottle(new FwLiteConfig { UpdateCheckCondition = UpdateCheckCondition.OnInterval });

        throttle.RecordCheck();

        throttle.LastUpdateCheck.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        throttle.ShouldCheckForUpdate().Should().BeFalse("because a check was just recorded");
    }
}
