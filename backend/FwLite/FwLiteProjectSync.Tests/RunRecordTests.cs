namespace FwLiteProjectSync.Tests;

public class RunRecordTests
{
    [Theory]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\r\n")]
    [InlineData("\f")]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    public void DescriptionStaysOnOneLine(string lineEnding)
    {
        var record = new RecordingMiniLcmApi.RunRecord("CreateEntry", $"first{lineEnding}second");

        record.Description.Should().Be("first\\nsecond");
    }
}
