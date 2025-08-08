using MiniLcm.SyncHelpers;
using SystemTextJsonPatch.Operations;

namespace FwLiteProjectSync.Tests;

public class IntegerDiffTests
{
    private record Placeholder();

    [Fact]
    public void DiffEmptyIntegersDoNothing()
    {
        int? before = null;
        int? after = null;
        var result = IntegerDiff.GetIntegerDiff<Placeholder>("test", before, after);
        result.Should().BeEmpty();
    }

    [Fact]
    public void DiffOneToEmptyAddsOne()
    {
        int? before = null;
        var after = 1;
        var result = IntegerDiff.GetIntegerDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("add", "/test", null, 1)
        ]);
    }

    [Fact]
    public void DiffOneToTwoReplacesOne()
    {
        var before = 1;
        var after = 2;
        var result = IntegerDiff.GetIntegerDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("replace", "/test", null, 2)
        ]);
    }

    [Fact]
    public void DiffNoneToOneRemovesOne()
    {
        var before = 1;
        int? after = null;
        var result = IntegerDiff.GetIntegerDiff<Placeholder>("test", before, after);
        result.Should().BeEquivalentTo([
            new Operation<Placeholder>("remove", "/test", null)
        ]);
    }
}
