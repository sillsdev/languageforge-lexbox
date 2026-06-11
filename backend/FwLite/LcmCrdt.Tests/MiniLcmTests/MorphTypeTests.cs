namespace LcmCrdt.Tests.MiniLcmTests;

public class MorphTypeTests : MorphTypeTestsBase
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

    // CRDT only, because fwdata does not support creating morph-types
    [Fact]
    public async Task CreateMorphType_Works()
    {
        var morphType = CanonicalMorphTypes.All.First().Value.Copy();
        var createdMorphType = await Api.CreateMorphType(morphType);
        createdMorphType.Should().NotBeNull();
        createdMorphType.Should().BeEquivalentTo(morphType);
    }
}
