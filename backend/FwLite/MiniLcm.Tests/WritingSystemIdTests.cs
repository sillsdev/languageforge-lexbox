namespace MiniLcm.Tests;

public class WritingSystemIdTests
{
    public static IEnumerable<object[]> ValidWritingSystemIds =>
        WritingSystemCodes.ValidTwoLetterCodes.Select(code => new object[] { code });

    [Theory]
    [MemberData(nameof(ValidWritingSystemIds))]
    [InlineData("en")]
    [InlineData("th")]
    [InlineData("xba")]
    [InlineData("en-Zxxx-x-audio")]
    public void ValidWritingSystemId_ShouldNotThrow(string code)
    {
        var ws = new WritingSystemId(code);
        ws.Should().NotBe(default);
    }

    [Theory]
    [InlineData("en-Zxxx-x-audio")]
    [InlineData("seh-Zxxx-x-audio-var")]
    public void DetectsAudioWritingSystems(string code)
    {
        var ws = new WritingSystemId(code);
        ws.IsAudio.Should().BeTrue();
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
        ws.Should().NotBe(default);
    }

    [Fact]
    public void DefaultStructHasDefaultCode()
    {
        WritingSystemId value = default;
        value.Code.Should().NotBeNull().And.Be("default");
    }
}
