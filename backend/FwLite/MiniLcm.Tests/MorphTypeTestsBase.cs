namespace MiniLcm.Tests;

public abstract class MorphTypeTestsBase : MiniLcmTestBase
{
    [Fact]
    public async Task GetMorphTypes_ReturnsAllCanonicalMorphTypes()
    {
        var morphTypes = await Api.GetMorphTypes().ToArrayAsync();
        morphTypes.Should().NotBeEmpty();
        morphTypes.Should().AllSatisfy(mt => mt.Id.Should().NotBe(Guid.Empty));
        // All canonical kinds (except Unknown) should be present
        var allKinds = Enum.GetValues<MorphTypeKind>().Where(k => k != MorphTypeKind.Unknown);
        morphTypes.Select(mt => mt.Kind).Should().BeEquivalentTo(allKinds);
    }

    [Fact]
    public async Task GetMorphType_ById_ReturnsExpected()
    {
        var morphTypes = await Api.GetMorphTypes().ToArrayAsync();
        var stem = morphTypes.First(mt => mt.Kind == MorphTypeKind.Stem);

        var result = await Api.GetMorphType(stem.Id);

        result.Should().NotBeNull();
        result!.Kind.Should().Be(MorphTypeKind.Stem);
        result.Id.Should().Be(stem.Id);
    }

    [Fact]
    public async Task GetMorphType_ByKind_ReturnsExpected()
    {
        var result = await Api.GetMorphType(MorphTypeKind.Prefix);

        result.Should().NotBeNull();
        result!.Kind.Should().Be(MorphTypeKind.Prefix);
    }

    [Fact]
    public async Task GetMorphType_ByKind_Unknown_ReturnsNull()
    {
        var result = await Api.GetMorphType(MorphTypeKind.Unknown);
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateMorphType_UpdatesName()
    {
        var stem = await Api.GetMorphType(MorphTypeKind.Stem);
        stem.Should().NotBeNull();

        var updatedStem = stem!.Copy();
        updatedStem.Name["en"] = "Updated Stem Name";
        await Api.UpdateMorphType(stem, updatedStem);

        var result = await Api.GetMorphType(MorphTypeKind.Stem);
        result.Should().NotBeNull();
        result!.Name["en"].Should().Be("Updated Stem Name");
    }

    [Fact]
    public async Task UpdateMorphType_UpdatesAbbreviation()
    {
        var prefix = await Api.GetMorphType(MorphTypeKind.Prefix);
        prefix.Should().NotBeNull();

        var updated = prefix!.Copy();
        updated.Abbreviation["en"] = "updated pfx";
        await Api.UpdateMorphType(prefix, updated);

        var result = await Api.GetMorphType(MorphTypeKind.Prefix);
        result.Should().NotBeNull();
        result!.Abbreviation["en"].Should().Be("updated pfx");
    }

    [Fact]
    public async Task UpdateMorphType_NoChanges_DoesNotThrow()
    {
        var stem = await Api.GetMorphType(MorphTypeKind.Stem);
        stem.Should().NotBeNull();

        var copy = stem!.Copy();
        // No changes made - should be a no-op
        await Api.UpdateMorphType(stem, copy);

        var result = await Api.GetMorphType(MorphTypeKind.Stem);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(stem);
    }
}
