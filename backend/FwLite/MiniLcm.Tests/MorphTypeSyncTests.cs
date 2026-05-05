using MiniLcm.SyncHelpers;

namespace MiniLcm.Tests;

public class MorphTypeSyncTests
{
    [Fact]
    public async Task Sync_ThrowsOnAdd_WhenAfterContainsExtraMorphType()
    {
        var before = CanonicalMorphTypes.All.Values.ToArray();
        var extraMorphType = new MorphType
        {
            Id = Guid.NewGuid(),
            Kind = MorphTypeKind.Unknown,
            Name = new MultiString { { "en", "bogus" } },
        };
        var after = before.Append(extraMorphType).ToArray();

        var act = () => MorphTypeSync.Sync(before, after, null!);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be created*");
    }

    [Fact]
    public async Task Sync_ThrowsOnRemove_WhenAfterIsMissingMorphType()
    {
        var before = CanonicalMorphTypes.All.Values.ToArray();
        var after = before.Skip(1).ToArray(); // drop one

        var act = () => MorphTypeSync.Sync(before, after, null!);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be deleted*");
    }
}
