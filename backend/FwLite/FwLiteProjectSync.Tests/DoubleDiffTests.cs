using MiniLcm.SyncHelpers;
using SystemTextJsonPatch.Operations;

namespace FwLiteProjectSync.Tests;

public class DoubleDiffTests
{
    private record Placeholder();

    [Fact]
    public void DiffEmptyDoublesDoNothing()
    {
        double? before = null;
        double? after = null;
        var result = DoubleDiff.GetDoubleDiff<Placeholder>("test", before, after);
        result.Should().BeEmpty();
    }

    [Fact]
    public void DiffOneToEmptyAddsOne()
    {
        double? before = null;
        var after = 1.5;
        var result = DoubleDiff.GetDoubleDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("add", "/test", null, 1.5)
        ]);
    }

    [Fact]
    public void DiffOneToTwoReplacesOne()
    {
        var before = 1.5;
        var after = 2.5;
        var result = DoubleDiff.GetDoubleDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("replace", "/test", null, 2.5)
        ]);
    }

    [Fact]
    public void DiffNoneToOneRemovesOne()
    {
        var before = 1.5;
        double? after = null;
        var result = DoubleDiff.GetDoubleDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("remove", "/test", null)
        ]);
    }
}
