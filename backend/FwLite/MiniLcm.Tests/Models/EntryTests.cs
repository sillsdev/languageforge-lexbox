namespace MiniLcm.Tests.Models;

public class EntryTests
{
    [Fact]
    public void Headword_SameResultForDifferentOrderedMultiStrings()
    {
        var entry = new Entry()
        {
            LexemeForm = new MultiString() { Values = { { "en", "test" }, { "fr", "test2" } } }
        };
        var entry2 = new Entry()
        {
            LexemeForm = new MultiString() { Values = { { "fr", "test2" }, { "en", "test" } } }
        };
        entry.Headword().Should().Be(entry2.Headword());
    }
}
