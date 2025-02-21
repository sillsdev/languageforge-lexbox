namespace MiniLcm.Tests;

public abstract class PublicationsTestsBase : MiniLcmTestBase
{
    private Guid _publicationId;
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Api.CreatePublication(new Publication() { Id = _publicationId = Guid.NewGuid(), Name = { { "en", "Main" } } });
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
}
