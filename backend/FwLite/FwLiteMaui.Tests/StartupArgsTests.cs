namespace FwLiteMaui.Tests;

public class StartupArgsTests
{
    [Theory]
    [InlineData("FwLiteMaui.exe --start-url /", "/")]
    [InlineData("FwLiteMaui.exe --start-url=/project/x/browse", "/project/x/browse")]
    [InlineData("FwLiteMaui.exe --other --start-url /a", "/a")]
    public void GetStartUrl_ReadsTheOption(string commandLine, string expected)
    {
        StartupArgs.GetStartUrl(commandLine.Split(' ')).Should().Be(expected);
    }

    [Theory]
    [InlineData("FwLiteMaui.exe")]
    [InlineData("FwLiteMaui.exe --start-url")]
    [InlineData("FwLiteMaui.exe --start-url https://example.com/")]
    [InlineData("FwLiteMaui.exe --start-url=")]
    public void GetStartUrl_IgnoresMissingOrNonRelativeValues(string commandLine)
    {
        StartupArgs.GetStartUrl(commandLine.Split(' ')).Should().BeNull();
    }
}
