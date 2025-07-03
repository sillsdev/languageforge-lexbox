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

        var entry = await Api.CreateEntry(new Entry() { Id = Guid.NewGuid(), PublishIn = []});
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
        var entry = await Api.CreateEntry(new Entry() { Id = Guid.NewGuid(), PublishIn = [publication] });

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
        actualPub.Should().BeEquivalentTo(afterPub, options => options.Excluding(c => c.Id));
    }
}
