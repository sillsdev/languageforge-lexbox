namespace MiniLcm.Tests.Models;

public class EntryTests
{
    [Fact]
    public void Headword_SameResultForDifferentOrderedMultiStrings()
    {
        var entry = new Entry()
        {
            LexemeForm = new MultiString() { { "en", "test" }, { "fr", "test2" } }
        };
        var entry2 = new Entry()
        {
            LexemeForm = new MultiString() { { "fr", "test2" }, { "en", "test" } }
        };
        entry.HeadwordText().Should().Be(entry2.HeadwordText());
    }
}
