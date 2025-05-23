using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using Spart.Parsers;
using SystemTextJsonPatch.Operations;

namespace FwLiteProjectSync.Tests;

public class MultiStringDiffTests
{
    private record Placeholder();

    [Fact]
    public void DiffEmptyDoesNothing()
    {
        var before = new MultiString();
        var after = new MultiString();
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after);
        result.Should().BeEmpty();
    }

    [Fact]
    public void DiffOneToEmptyAddsOne()
    {
        var before = new MultiString();
        var after = new MultiString { { "en", "hello" } };
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("add", "/test/en", null, "hello")
        ]);
    }

    [Fact]
    public void DiffOneToOneReplacesOne()
    {
        var before = new MultiString { { "en", "hello" } };
        var after = new MultiString { { "en", "world" } };
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("replace", "/test/en", null, "world")
        ]);
    }

    [Fact]
    public void DiffNoneToOneRemovesOne()
    {
        var before = new MultiString { { "en", "hello" } };
        var after = new MultiString();
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("remove", "/test/en", null)
        ]);
    }

    [Fact]
    public void DiffMatchingDoesNothing()
    {
        var before = new MultiString { { "en", "hello" } };
        var after = new MultiString { { "en", new string(['h', 'e', 'l', 'l', 'o']) } };
        //ensure the strings are not the same instance
        ReferenceEquals(before["en"], after["en"]).Should().BeFalse();
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after);
        result.Should().BeEmpty();
    }

    [Fact]
    public void DiffWithMultipleAddsRemovesEach()
    {
        var before = new MultiString
        {
            { "en", "hello" },
            { "es", "hola" }
        };
        var after = new MultiString
        {
            { "en", "world" },
            { "fr", "monde" }
        };
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("replace", "/test/en", null, "world"),
            new Operation<Placeholder>("add", "/test/fr", null, "monde"),
            new Operation<Placeholder>("remove", "/test/es", null)
        ]);
    }

    [Fact]
    public void Rich_DiffMatchingAppliesPropChanges()
    {
        var before = new RichMultiString
        {
            { "en", new RichString([new RichSpan() { Text = "hello", Bold = RichTextToggle.On }]) }
        };
        var after = new RichMultiString
        {
            { "en", new RichString([new RichSpan() { Text = "hello", Bold = RichTextToggle.Off }]) }
        };
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("replace", "/test/en", null,
                new RichString([new RichSpan() { Text = "hello", Bold = RichTextToggle.Off }])),
        ]);
    }

    [Fact]
    public void Rich_DiffSameDoesNothing()
    {
        var before = new RichMultiString
        {
            { "en", new RichString([new RichSpan() { Text = "hello", Bold = RichTextToggle.On }]) }
        };
        var after = new RichMultiString
        {
            { "en", new RichString([new RichSpan() { Text = "hello", Bold = RichTextToggle.On }]) }
        };
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after);
        result.Should().BeEmpty();
    }
}
