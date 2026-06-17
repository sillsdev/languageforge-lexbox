using LcmCrdt.Changes;
using MiniLcm.SyncHelpers;

namespace LcmCrdt.Tests.MiniLcmTests;

public class PublicationsTests : PublicationsTestsBase
{
    private readonly MiniLcmApiFixture _fixture = new();

    protected override async Task<IMiniLcmApi> NewApi()
    {
        await _fixture.InitializeAsync();
        var api = _fixture.Api;
        return api;
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task UpdatePublication_CannotTurnOffIsMain()
    {
        var main = await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Main" } }, IsMain = true });

        var act = () => Api.UpdatePublication(main.Id, new UpdateObjectInput<Publication>().Set(p => p.IsMain, false));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdatePublication_SettingIsMainFalseOnNonMainIsNoOp()
    {
        var pub = await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Pocket" } } });

        await Api.UpdatePublication(pub.Id, new UpdateObjectInput<Publication>().Set(p => p.IsMain, false));

        var updated = await Api.GetPublication(pub.Id);
        ArgumentNullException.ThrowIfNull(updated);
        updated.IsMain.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePublication_CannotPromoteSecondMain()
    {
        await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Main" } }, IsMain = true });
        var other = await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Pocket" } } });

        var act = () => Api.UpdatePublication(other.Id, new UpdateObjectInput<Publication>().Set(p => p.IsMain, true));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreatePublication_CannotCreateSecondMain()
    {
        await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Main" } }, IsMain = true });

        var act = () => Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Second" } }, IsMain = true });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PublicationSync_PromotesExistingPublicationToMain()
    {
        var pub = await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Pocket" } } });
        var promoted = new Publication { Id = pub.Id, Name = { { "en", "Pocket" } }, IsMain = true };

        await PublicationSync.Sync([pub], [promoted], Api);

        var updated = await Api.GetPublication(pub.Id);
        ArgumentNullException.ThrowIfNull(updated);
        updated.IsMain.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePublication_SecondMainConvergesToNonMain()
    {
        var firstMainId = Guid.NewGuid();
        var secondMainId = Guid.NewGuid();
        // Apply both main-creates directly (bypassing the API guard) to simulate two replicas converging on merge.
        await _fixture.DataModel.AddChange(Guid.NewGuid(), new CreatePublicationChange(firstMainId, new MultiString { { "en", "Main" } }, isMain: true));
        await _fixture.DataModel.AddChange(Guid.NewGuid(), new CreatePublicationChange(secondMainId, new MultiString { { "en", "Other main" } }, isMain: true));

        var publications = await Api.GetPublications().ToArrayAsync();
        publications.Should().ContainSingle(p => p.IsMain).Which.Id.Should().Be(firstMainId);
        // The duplicate survives as a regular (non-main) publication rather than being deleted.
        publications.Should().ContainSingle(p => p.Id == secondMainId).Which.IsMain.Should().BeFalse();
    }
}
