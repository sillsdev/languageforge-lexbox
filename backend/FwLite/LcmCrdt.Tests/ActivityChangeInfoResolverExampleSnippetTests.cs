using System.Globalization;

namespace LcmCrdt.Tests;

public class ActivityChangeInfoResolverExampleSnippetTests
{
    private static RichMultiString Sentence(string text) => new() { ["en"] = new RichString(text) };

    private const int Budget = ActivityChangeInfoResolver.TextSnippetBudget;

    [Theory]
    // Fits within the budget → returned whole, no ellipsis.
    [InlineData("I run every morning", "I run every morning")] // 19 chars
    [InlineData("12345678901234567890", "12345678901234567890")] // exactly 20
    // Over budget → cut back to the last space in the kept window (so a word isn't split), then ellipsis.
    [InlineData("The quick brown fox jumps over the lazy dog", "The quick brown fox…")] // 4 words
    // A single long token with no space to break on → hard grapheme cut at the budget.
    [InlineData("Supercalifragilisticexpialidocious is long", "Supercalifragilistic…")]
    public void ExampleSnippet_TruncatesSpacedText(string input, string expected)
    {
        ActivityChangeInfoResolver.ExampleSnippet(Sentence(input)).Should().Be(expected);
    }

    [Fact]
    public void ExampleSnippet_RespectsWritingSystemOrder()
    {
        // "fr" is configured first, so it wins even though "en" sorts earlier by code.
        var sentence = new RichMultiString { ["en"] = new RichString("hello"), ["fr"] = new RichString("bonjour") };
        var wsOrder = new Dictionary<WritingSystemId, int> { ["fr"] = 0, ["en"] = 1 };
        ActivityChangeInfoResolver.ExampleSnippet(sentence, wsOrder).Should().Be("bonjour");
    }

    [Fact]
    public void ExampleSnippet_FallsBackToWritingSystemCode_WhenNoOrderGiven()
    {
        // With no project writing-system order supplied, selection falls back to WS code; "en" sorts before "fr".
        var sentence = new RichMultiString { ["fr"] = new RichString("bonjour"), ["en"] = new RichString("hello") };
        ActivityChangeInfoResolver.ExampleSnippet(sentence).Should().Be("hello");
    }

    [Fact]
    public void ExampleSnippet_FlattensSpansAndCollapsesWhitespace()
    {
        var sentence = new RichMultiString
        {
            ["en"] = new RichString([
                new RichSpan { Text = "I ", Bold = RichTextToggle.On },
                new RichSpan { Text = "run\n\n  daily" },
            ]),
        };
        ActivityChangeInfoResolver.ExampleSnippet(sentence).Should().Be("I run daily");
    }

    [Fact]
    public void ExampleSnippet_SkipsEmptyWritingSystems()
    {
        var sentence = new RichMultiString { ["en"] = new RichString("   "), ["seh"] = new RichString("nyumba") };
        ActivityChangeInfoResolver.ExampleSnippet(sentence).Should().Be("nyumba");
    }

    [Fact]
    public void ExampleSnippet_NoDisplayableText_ReturnsNull()
    {
        ActivityChangeInfoResolver.ExampleSnippet(new RichMultiString()).Should().BeNull();
        ActivityChangeInfoResolver.ExampleSnippet(new RichMultiString { ["en"] = new RichString("") }).Should().BeNull();
    }

    [Fact]
    public void ExampleSnippet_NoSpaceScript_HardCutsAtGraphemeBoundary()
    {
        // CJK has no inter-word spaces; there's nothing to break on, so it's a clean cut at the budget.
        var text = new string('好', 25);
        ActivityChangeInfoResolver.ExampleSnippet(Sentence(text)).Should().Be(new string('好', 20) + "…");
    }

    [Fact]
    public void ExampleSnippet_CombiningMarks_NotSplitMidGrapheme()
    {
        // "a" + combining acute accent = one grapheme, two chars. Truncation must count graphemes, not chars,
        // and never cut between the base and its mark.
        var text = string.Concat(Enumerable.Repeat("á", Budget + 5)); // 25 graphemes, 50 chars
        var result = ActivityChangeInfoResolver.ExampleSnippet(Sentence(text));

        result.Should().EndWith("…");
        var kept = result[..^1];
        new StringInfo(kept).LengthInTextElements.Should().Be(Budget);
        kept.Length.Should().Be(Budget * 2); // 20 graphemes × 2 chars each, i.e. no pair was split
    }

    [Fact]
    public void ExampleSnippet_SurrogatePairs_NotSplit()
    {
        // Emoji outside the BMP are surrogate pairs (two chars, one grapheme); a char-based cut would corrupt them.
        var text = string.Concat(Enumerable.Repeat("😀", count: Budget + 5));
        var result = ActivityChangeInfoResolver.ExampleSnippet(Sentence(text));

        result.Should().Be(string.Concat(Enumerable.Repeat("😀", Budget)) + "…");
    }
}
