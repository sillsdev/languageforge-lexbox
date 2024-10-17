using FwLiteProjectSync.SyncHelpers;
using MiniLcm.Models;
using Spart.Parsers;
using SystemTextJsonPatch.Operations;

namespace FwLiteProjectSync.Tests;

public class MultiStringDiffTests
{
    private record Placeholder();

    [Fact]
    public void DiffEmptyDoesNothing()
    {
        var previous = new MultiString();
        var current = new MultiString();
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", previous, current);
        result.Should().BeEmpty();
    }

    [Fact]
    public void DiffOneToEmptyAddsOne()
    {
        var previous = new MultiString();
        var current = new MultiString();
        current.Values.Add("en", "hello");
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", previous, current);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("add", "/test/en", null, "hello")
        ]);
    }

    [Fact]
    public void DiffOneToOneReplacesOne()
    {
        var previous = new MultiString();
        previous.Values.Add("en", "hello");
        var current = new MultiString();
        current.Values.Add("en", "world");
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", previous, current);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("replace", "/test/en", null, "world")
        ]);
    }

    [Fact]
    public void DiffNoneToOneRemovesOne()
    {
        var previous = new MultiString();
        previous.Values.Add("en", "hello");
        var current = new MultiString();
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", previous, current);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("remove", "/test/en", null)
        ]);
    }

    [Fact]
    public void DiffMatchingDoesNothing()
    {
        var previous = new MultiString();
        previous.Values.Add("en", "hello");
        var current = new MultiString();
        current.Values.Add("en", new string(['h', 'e', 'l', 'l', 'o']));
        //ensure the strings are not the same instance
        ReferenceEquals(previous["en"], current["en"]).Should().BeFalse();
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", previous, current);
        result.Should().BeEmpty();
    }

    [Fact]
    public void DiffWithMultipleAddsRemovesEach()
    {
        var previous = new MultiString();
        previous.Values.Add("en", "hello");
        previous.Values.Add("es", "hola");
        var current = new MultiString();
        current.Values.Add("en", "world");
        current.Values.Add("fr", "monde");
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", previous, current);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("replace", "/test/en", null, "world"),
            new Operation<Placeholder>("add", "/test/fr", null, "monde"),
            new Operation<Placeholder>("remove", "/test/es", null)
        ]);
    }
}
