using MiniLcm.SyncHelpers;
using SystemTextJsonPatch.Operations;

namespace FwLiteProjectSync.Tests;

public class SimpleStringDiffTests
{
    private record Placeholder();

    [Fact]
    public void DiffEmptyStringsDoNothing()
    {
        string? before = null;
        string? after = null;
        var result = SimpleStringDiff.GetStringDiff<Placeholder>("test", before, after);
        result.Should().BeEmpty();
    }

    [Fact]
    public void DiffOneToEmptyAddsOne()
    {
        string? before = null;
        var after = "hello";
        var result = SimpleStringDiff.GetStringDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("add", "/test", null, "hello")
        ]);
    }

    [Fact]
    public void DiffOneToOneReplacesOne()
    {
        var before = "hello";
        var after = "world";
        var result = SimpleStringDiff.GetStringDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("replace", "/test", null, "world")
        ]);
    }

    [Fact]
    public void DiffNoneToOneRemovesOne()
    {
        var before = "hello";
        string? after = null;
        var result = SimpleStringDiff.GetStringDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("remove", "/test", null)
        ]);
    }
}
