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
        entry.Headword().Should().Be(entry2.Headword());
    }

    [Theory]
    [InlineData("bank", 0, "bank")]
    [InlineData("bank", 2, "bank₂")]
    [InlineData("bank", 12, "bank₁₂")]
    [InlineData("", 1, "")]
    [InlineData(Entry.UnknownHeadword, 1, Entry.UnknownHeadword)]
    public void WithHomographNumber_AppendsSubscriptDigits(string headword, int homographNumber, string expected)
    {
        Entry.WithHomographNumber(headword, homographNumber).Should().Be(expected);
    }
}
