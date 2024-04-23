using Shouldly;
using Testing.SyncReverseProxy;

namespace Testing.Services;

public class UtilsTests
{
    [Theory]
    [InlineData(nameof(SendReceiveServiceTests.ModifyProjectData), "modify-project-data")]
    [InlineData(nameof(SendReceiveServiceTests.SendNewProject), "send-new-project")]
    [InlineData($"{nameof(SendReceiveServiceTests.SendNewProject)} (res)", "send-new-project-res")]
    [InlineData("_DashAround__123theBlock", "dash-around-123the-block")]
    [InlineData("SimultaneousResetsDontResultIn404s", "simultaneous-resets-dont-result-in-404s")]
    [InlineData("SimultaneousResetsDontResultIn404S", "simultaneous-resets-dont-result-in-404-s")]
    public void VerifyToProjectCodeFriendlyString(string input, string expected)
    {
        Utils.ToProjectCodeFriendlyString(input).ShouldBe(expected);
    }
}
