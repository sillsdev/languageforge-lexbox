namespace MiniLcm.Tests;

public class JsonPatchExtensionsTests
{
    [Fact]
    public void SummarizeJoinsMultipleOperations()
    {
        var update = new UpdateObjectInput<WritingSystem>()
            .Set(ws => ws.Name, "English")
            .Set(ws => ws.Abbreviation, "en");

        update.Summarize().Should().Be("Replace /Name: English, Replace /Abbreviation: en");
    }
}
