namespace MiniLcm.Tests;

public class JsonPatchExtensionsTests
{
    [Theory]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\r\n")]
    [InlineData("\f")]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    public void SummarizeStaysOnOneLine(string lineEnding)
    {
        var update = new UpdateObjectInput<WritingSystem>()
            .Set(ws => ws.Name, $"first{lineEnding}second");

        var summary = update.Summarize();

        summary.Should().Be("Replace /Name: first\\nsecond");
    }

    [Fact]
    public void SummarizeJoinsMultipleOperations()
    {
        var update = new UpdateObjectInput<WritingSystem>()
            .Set(ws => ws.Name, "English")
            .Set(ws => ws.Abbreviation, "en");

        update.Summarize().Should().Be("Replace /Name: English, Replace /Abbreviation: en");
    }
}
