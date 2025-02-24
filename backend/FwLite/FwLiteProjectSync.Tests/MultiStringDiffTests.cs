using MiniLcm.Exceptions;
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
        var after = new MultiString();
        after.Values.Add("en", "hello");
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("add", "/test/en", null, "hello")
        ]);
    }

    [Fact]
    public void DiffOneToOneReplacesOne()
    {
        var before = new MultiString();
        before.Values.Add("en", "hello");
        var after = new MultiString();
        after.Values.Add("en", "world");
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("replace", "/test/en", null, "world")
        ]);
    }

    [Fact]
    public void DiffNoneToOneRemovesOne()
    {
        var before = new MultiString();
        before.Values.Add("en", "hello");
        var after = new MultiString();
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("remove", "/test/en", null)
        ]);
    }

    [Fact]
    public void DiffMatchingDoesNothing()
    {
        var before = new MultiString();
        before.Values.Add("en", "hello");
        var after = new MultiString();
        after.Values.Add("en", new string(['h', 'e', 'l', 'l', 'o']));
        //ensure the strings are not the same instance
        ReferenceEquals(before["en"], after["en"]).Should().BeFalse();
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after);
        result.Should().BeEmpty();
    }

    [Fact]
    public void DiffWithMultipleAddsRemovesEach()
    {
        var before = new MultiString();
        before.Values.Add("en", "hello");
        before.Values.Add("es", "hola");
        var after = new MultiString();
        after.Values.Add("en", "world");
        after.Values.Add("fr", "monde");
        var result = MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("replace", "/test/en", null, "world"),
            new Operation<Placeholder>("add", "/test/fr", null, "monde"),
            new Operation<Placeholder>("remove", "/test/es", null)
        ]);
    }

    [Fact]
    public void DiffAttemptToUpdateReadonlyFails()
    {
        var before = new MultiString();
        before.Values.Add("en", "hello");
        before.Metadata.Add("en", new(){RunCount = 2});
        var after = new MultiString();
        after.Values.Add("en", "world");
        var act = () =>
        {
            MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after).ToArray();
        };
        act.Should().Throw<MultiStringReadonlyException>();
    }

    [Fact]
    public void DiffWithoutChangeToReadonlyStringWorks()
    {
        var before = new MultiString();
        before.Values.Add("en", "hello");
        before.Values.Add("es", "hola");
        before.Metadata.Add("en", new(){RunCount = 2});
        var after = new MultiString();
        after.Values.Add("en", "hello");
        var act = () =>
        {
            MultiStringDiff.GetMultiStringDiff<Placeholder>("test", before, after).ToArray();
        };
        act.Should().NotThrow();
    }
}
