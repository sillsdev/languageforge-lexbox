using MiniLcm.Culture;

namespace MiniLcm.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("a", false)]
    [InlineData("á", true)]
    [InlineData("á", true)]
    [InlineData("\U0001E922\U0001E944", true)]//Adlam letter + alif lengthener, a mark outside the BMP
    [InlineData("\U0001E922", false)]
    public void ContainsDiacritic(string value, bool expected)
    {
        StringExtensions.ContainsDiacritic(value).Should().Be(expected);
    }
}
