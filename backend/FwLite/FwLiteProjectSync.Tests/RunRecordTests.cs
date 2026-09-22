namespace FwLiteProjectSync.Tests;

public class RunRecordTests
{
    [Theory]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\r\n")]
    public void DescriptionStaysOnOneLine(string lineEnding)
    {
        var record = new RecordingMiniLcmApi.RunRecord("CreateEntry", $"first{lineEnding}second");

        record.Description.Should().Be("first\\nsecond");
    }
}
