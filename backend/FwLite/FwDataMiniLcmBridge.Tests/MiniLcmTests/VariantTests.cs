using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class VariantTests(ProjectLoaderFixture fixture) : VariantTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("variant-test", "en", "en"));
    }

    [Fact]
    public async Task GetVariantTypes_IncludesIrregularlyInflectedFormSubtypes()
    {
        // Plural and Past are children of Irregularly Inflected Form in the possibility
        // list; the flattened read must surface them (they're the headline use case)
        var types = await Api.GetVariantTypes().ToArrayAsync();
        types.Should().Contain(t => t.Id == LexEntryTypeTags.kguidLexTypPluralVar);
        types.Should().Contain(t => t.Id == LexEntryTypeTags.kguidLexTypPastVar);
        types.Should().Contain(t => t.Id == LexEntryTypeTags.kguidLexTypSpellingVar);
    }

    [Fact]
    public async Task CreateVariantType_LandsAsATopLevelPossibility()
    {
        // the flattened read can surface children, but created types must be top-level
        var created = await Api.CreateVariantType(new VariantType { Id = Guid.NewGuid(), Name = new() { { "en", "custom type" } } });
        var fwDataApi = (FwDataMiniLcmApi)BaseApi;
        fwDataApi.Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS
            .Should().Contain(p => p.Guid == created.Id);
    }
}

/// <summary>
/// tests the per-link fidelity when FwData has multiple variant LexEntryRefs on one entry
/// </summary>
[Collection(ProjectLoaderFixture.Name)]
public class VariantTestsMultipleRefs(ProjectLoaderFixture fixture) : VariantTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("variant-test-multipleRefs", "en", "en"));
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var fwDataApi = (FwDataMiniLcmApi)BaseApi;
        var variantEntry = fwDataApi.EntriesRepository.GetObject(_variantEntryId);
        await fwDataApi.Cache.DoUsingNewOrCurrentUOW("Add dangling variant LexEntryRefs",
            "Remove dangling variant LexEntryRefs",
            () =>
            {
                //FLEx data can contain variant refs with no components; they must not confuse reads or writes
                foreach (var _ in Enumerable.Range(0, 2))
                {
                    var entryRef = fwDataApi.Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
                    variantEntry.EntryRefsOS.Add(entryRef);
                    entryRef.RefType = LexEntryRefTags.krtVariant;
                }
                return ValueTask.CompletedTask;
            });
    }

    [Fact]
    public async Task VariantRefsWithDifferentTypes_ReadAsSeparateLinksWithOwnTypes()
    {
        var otherMainEntry = await Api.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "other main" } }
        });
        var typeA = await Api.CreateVariantType(new VariantType { Id = Guid.NewGuid(), Name = new() { { "en", "type a" } } });
        var typeB = await Api.CreateVariantType(new VariantType { Id = Guid.NewGuid(), Name = new() { { "en", "type b" } } });

        await Api.CreateVariant(Variant.FromEntries(_variantEntry, _mainEntry) with { Types = [typeA] });
        await Api.CreateVariant(Variant.FromEntries(_variantEntry, otherMainEntry) with { Types = [typeB] });

        var entry = await Api.GetEntry(_variantEntryId);
        entry!.VariantOf.Should().HaveCount(2);
        entry.VariantOf.Should().ContainSingle(v => v.MainEntryId == _mainEntryId)
            .Which.Types.Should().ContainSingle(t => t.Id == typeA.Id);
        entry.VariantOf.Should().ContainSingle(v => v.MainEntryId == otherMainEntry.Id)
            .Which.Types.Should().ContainSingle(t => t.Id == typeB.Id);
    }

    [Fact]
    public async Task DuplicateVariantRefs_AreNotDuplicated()
    {
        await AddDuplicateVariantRef();

        var entry = await Api.GetEntry(_variantEntryId);
        entry.Should().NotBeNull();
        entry!.VariantOf.Should().HaveCount(1);
        var mainEntry = await Api.GetEntry(_mainEntryId);
        mainEntry.Should().NotBeNull();
        mainEntry!.Variants.Should().HaveCount(1);
    }

    [Fact]
    public async Task DuplicateVariantRefs_BothAreRemoved()
    {
        await AddDuplicateVariantRef();

        var entry = await Api.GetEntry(_variantEntryId);
        await Api.DeleteVariant(entry!.VariantOf.Single());

        entry = await Api.GetEntry(_variantEntryId);
        entry.Should().NotBeNull();
        entry!.VariantOf.Should().BeEmpty();
    }

    private async Task AddDuplicateVariantRef()
    {
        var fwDataApi = (FwDataMiniLcmApi)BaseApi;
        var variantEntry = fwDataApi.EntriesRepository.GetObject(_variantEntryId);
        var mainEntry = fwDataApi.EntriesRepository.GetObject(_mainEntryId);
        await fwDataApi.Cache.DoUsingNewOrCurrentUOW("Add variant LexEntryRefs",
            "Remove variant LexEntryRefs",
            () =>
            {
                variantEntry.MakeVariantOf(mainEntry, fwDataApi.VariantTypesFlattened.First());
                variantEntry.MakeVariantOf(mainEntry, fwDataApi.VariantTypesFlattened.Last());
                return ValueTask.CompletedTask;
            });
    }
}
