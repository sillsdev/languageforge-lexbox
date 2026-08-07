namespace MiniLcm.Tests;

public abstract class PublicationsTestsBase : MiniLcmTestBase
{
    private Guid _publicationId;
    private Guid _publicationId2;
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Api.CreatePublication(new Publication() { Id = _publicationId = Guid.NewGuid(), Name = { { "en", "Main" } } });
        await Api.CreatePublication(new Publication() { Id = _publicationId2 = Guid.NewGuid(), Name = { { "en", "Crossword" } } });
    }

    [Fact]
    public async Task GetPublications_ReturnsPublications()
    {
        var publications = await Api.GetPublications().ToArrayAsync();
        publications.Should()
            .NotBeEmpty()
            .And
            .AllSatisfy(po => po.Id.Should().NotBe(Guid.Empty));
    }

    [Fact]
    public async Task GetPublication_ReturnsPublication()
    {
        var publication = await Api.GetPublication(_publicationId);
        publication.Should().NotBeNull();
    }

    [Fact]
    public async Task DeletePublication_DeletesPublication()
    {
        Publication? publication = await Api.GetPublication(_publicationId);
        publication.Should().NotBeNull();
        await Api.DeletePublication(_publicationId);
        publication = await Api.GetPublication(_publicationId);
        publication.Should().BeNull();
    }

    [Fact]
    public async Task CreateEntry_WithPublication_ReturnsEntryWithPublication()
    {
        var publication = await Api.GetPublication(_publicationId);
        var publication2 = await Api.GetPublication(_publicationId2);
        ArgumentNullException.ThrowIfNull(publication);
        ArgumentNullException.ThrowIfNull(publication2);

        var entry = await Api.CreateEntry(new Entry() { Id = Guid.NewGuid(), PublishIn = [publication]});
        entry.PublishIn.Should().ContainEquivalentOf(publication);
        entry.PublishIn.Should().NotContainEquivalentOf(publication2);
    }

    [Fact]
    public async Task CreateEntry_WithOutPublication_ReturnsEntryWithOutPublication()
    {
        var publication = await Api.GetPublication(_publicationId);
        var publication2 = await Api.GetPublication(_publicationId2);
        ArgumentNullException.ThrowIfNull(publication);
        ArgumentNullException.ThrowIfNull(publication2);

        var entry = await Api.CreateEntry(new Entry() { Id = Guid.NewGuid(), PublishIn = [] }, CreateEntryOptions.AsIs);
        entry.PublishIn.Should().BeEmpty();
    }

    [Fact]
    public async Task AddPublication_UpdatesTheEntry()
    {
        var publication = await Api.GetPublication(_publicationId);
        ArgumentNullException.ThrowIfNull(publication);
        var entry = await Api.CreateEntry(new Entry() { Id = Guid.NewGuid(), PublishIn = [] });

        await Api.AddPublication(entry.Id, _publicationId);

        entry = await Api.GetEntry(entry.Id);
        entry.Should().NotBeNull();
        entry.PublishIn.Should().ContainEquivalentOf(publication);
    }

    [Fact]
    public async Task RemovePublication_UpdatesTheEntry()
    {

        var publication = await Api.GetPublication(_publicationId);
        ArgumentNullException.ThrowIfNull(publication);
        var entry = await Api.CreateEntry(new Entry() { Id = Guid.NewGuid(), PublishIn = [publication] }, CreateEntryOptions.AsIs);

        await Api.RemovePublication(entry.Id, _publicationId);

        entry = await Api.GetEntry(entry.Id);
        entry.Should().NotBeNull();
        entry.PublishIn.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdatePublication_WithUpdateObject_Works()
    {
        var publication = new Publication() { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } };
        await Api.CreatePublication(publication);
        var updatedPublication = await Api.UpdatePublication(publication.Id, new UpdateObjectInput<Publication>().Set(c => c.Name["en"], "updated"));
        updatedPublication.Name["en"].Should().Be("updated");
    }

    [Fact]
    public async Task UpdatePublication_WithBeforeAndAfter_Works()
    {
        var publication = new Publication() { Id = Guid.NewGuid(), Name = new() { { "en", "test" } } };
        await Api.CreatePublication(publication);
        var afterPub = new Publication() { Id = publication.Id, Name = new() { { "en", "updated" } } };
        var actualPub = await Api.UpdatePublication(publication, afterPub);
        actualPub.Should().BeEquivalentTo(afterPub);
    }

    [Fact]
    public async Task CreatePublication_CannotCreateSecondMain()
    {
        await GetOrCreateMainPublication();

        var act = () => Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Second" } }, IsMain = true });

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task UpdatePublication_CannotPromoteSecondMain()
    {
        await GetOrCreateMainPublication();
        var other = await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Pocket" } } });

        var act = () => Api.UpdatePublication(other.Id, new UpdateObjectInput<Publication>().Set(p => p.IsMain, true));

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task UpdatePublication_CannotTurnOffIsMain()
    {
        var main = await GetOrCreateMainPublication();

        var act = () => Api.UpdatePublication(main.Id, new UpdateObjectInput<Publication>().Set(p => p.IsMain, false));

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task UpdatePublication_WithBeforeAndAfter_CannotPromoteSecondMain()
    {
        await GetOrCreateMainPublication();
        var other = await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Pocket" } } });
        var promoted = other.Copy();
        promoted.IsMain = true;

        var act = () => Api.UpdatePublication(other, promoted);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task UpdatePublication_WithBeforeAndAfter_CannotTurnOffIsMain()
    {
        var main = await GetOrCreateMainPublication();
        var demoted = main.Copy();
        demoted.IsMain = false;

        var act = () => Api.UpdatePublication(main, demoted);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task SubmitUpdatePublication_CannotTurnOffIsMain()
    {
        var main = await GetOrCreateMainPublication();

        var act = () => Api.SubmitUpdatePublication(main.Id, new UpdateObjectInput<Publication>().Set(p => p.IsMain, false));

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task SubmitUpdatePublication_CannotPromoteSecondMain()
    {
        await GetOrCreateMainPublication();
        var other = await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Pocket" } } });

        var act = () => Api.SubmitUpdatePublication(other.Id, new UpdateObjectInput<Publication>().Set(p => p.IsMain, true));

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
