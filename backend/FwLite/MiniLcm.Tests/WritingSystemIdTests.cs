using MiniLcm.Models;

namespace MiniLcm.Tests;

public class WritingSystemIdTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("th")]
    [InlineData("en-Zxxx-x-audio")]
    public void ValidWritingSystemId_ShouldNotThrow(string code)
    {
        var ws = new WritingSystemId(code);
        ws.Should().NotBeNull();
    }

    [Theory]
    [InlineData("gx")]
    [InlineData("oo")]
    [InlineData("eng")] // Three-letter codes not allowed when there's a valid two-letter code
    [InlineData("eng-Zxxx-x-audio")]
    [InlineData("nonsense")]
    public void InvalidWritingSystemId_ShouldThrow(string code)
    {
        Assert.Throws<ArgumentException>(() => new WritingSystemId(code));
    }

    [Fact]
    public void DefaultWritingSystemId_IsValid()
    {
        var ws = new WritingSystemId("default");
        ws.Should().NotBeNull();
    }
}
