using LexCore.Entities;
using FluentAssertions;

namespace Testing.LexCore;

public class ProjectCodeTests
{
    [Theory]
    [InlineData("_____deleted_____")]
    [InlineData("_____temp_____")]
    [InlineData("api")]
    [InlineData("../hacker")]
    [InlineData("hacker/test")]
    [InlineData("/hacker")]
    [InlineData(@"hacker\test")]
    [InlineData("❌")]
    [InlineData("!")]
    [InlineData("#")]
    [InlineData("-not-start-with-dash")]
    public void InvalidCodesThrows(string code)
    {
        Assert.Throws<ArgumentException>(() => new ProjectCode(code));
    }

    [Theory]
    [InlineData("test-name123")]
    [InlineData("123-name")]
    [InlineData("test")]
    public void ValidCodes(string code)
    {
        var projectCode = new ProjectCode(code);
        projectCode.Value.Should().Be(code);
        projectCode.ToString().Should().Be(code);
    }
}
