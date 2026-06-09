using MiniLcm.SyncHelpers;
using Moq;

namespace MiniLcm.Tests;

public class MorphTypeSyncTests
{
    [Fact]
    public async Task Sync_CreatesMorphType_WhenAfterContainsExtraMorphType()
    {
        var before = CanonicalMorphTypes.All.Values.ToArray();
        var extraMorphType = new MorphType
        {
            Id = Guid.NewGuid(),
            Kind = MorphTypeKind.Unknown,
            Name = new MultiString { { "en", "bogus" } },
        };
        var after = before.Append(extraMorphType).ToArray();
        var api = new Mock<IMiniLcmApi>();
        api.Setup(a => a.CreateMorphType(It.IsAny<MorphType>())).ReturnsAsync((MorphType mt) => mt);

        var changeCount = await MorphTypeSync.Sync(before, after, api.Object);

        changeCount.Should().Be(1);
        api.Verify(a => a.CreateMorphType(It.Is<MorphType>(mt => mt.Id == extraMorphType.Id)), Times.Once);
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
