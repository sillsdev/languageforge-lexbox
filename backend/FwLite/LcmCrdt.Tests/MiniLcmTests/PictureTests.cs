namespace LcmCrdt.Tests.MiniLcmTests;

public class PictureTests : PictureTestsBase
{
    private readonly MiniLcmApiFixture _fixture = new();

    protected override async Task<IMiniLcmApi> NewApi()
    {
        await _fixture.InitializeAsync("picture-test");
        return _fixture.Api;
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task CreateSense_NumbersPicturesFromOne()
    {
        var sense = await CreateSenseWith(PicturesContradictingListOrder());
        sense.Pictures.Select(p => p.Order).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task CreateEntry_NumbersPicturesFromOne()
    {
        var sense = await CreateEntryWith(PicturesContradictingListOrder());
        sense.Pictures.Select(p => p.Order).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task BulkCreateEntries_NumbersPicturesFromOne()
    {
        var sense = await BulkCreateEntryWith(PicturesContradictingListOrder());
        sense.Pictures.Select(p => p.Order).Should().Equal(1, 2, 3);
    }
}
