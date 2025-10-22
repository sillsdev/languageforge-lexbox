using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Tests.Fixtures;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class ComplexFormComponentTests(ProjectLoaderFixture fixture): ComplexFormComponentTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("complex-form-component-test", "en", "en"));
    }
}

/// <summary>
/// used to test the case where FwData has multiple ComplexFormEntryRefs in ILexEntry
/// </summary>
[Collection(ProjectLoaderFixture.Name)]
public class ComplexFormComponentTestsMultipleRefs(ProjectLoaderFixture fixture): ComplexFormComponentTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("complex-form-component-test-multipleRefs", "en", "en"));
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var fwDataApi = (FwDataMiniLcmApi)BaseApi;
        var complexFormEntry = fwDataApi.EntriesRepository.GetObject(_complexFormEntryId);
        await fwDataApi.Cache.DoUsingNewOrCurrentUOW("Add ComplexFormEntryRef",
            "Remove ComplexFormEntryRef",
            () =>
            {
                //the entry is not a complex form yet, so we need to add 2
                fwDataApi.AddComplexFormLexEntryRef(complexFormEntry);
                fwDataApi.AddComplexFormLexEntryRef(complexFormEntry);
                return ValueTask.CompletedTask;
            });
    }

    [Fact]
    public async Task DuplicateComplexFormComponents_AreNotDuplicated()
    {
        await AddDuplicateComponent();

        var entry = await Api.GetEntry(_complexFormEntryId);
        entry.Should().NotBeNull();
        entry.Components.Should().HaveCount(1);
        var component = await Api.GetEntry(_componentEntryId);
        component.Should().NotBeNull();
        component.ComplexForms.Should().HaveCount(1);
    }


    [Fact]
    public async Task DuplicateComplexFormComponents_BothAreRemoved()
    {
        await AddDuplicateComponent();

        var entry = await Api.GetEntry(_complexFormEntryId);
        await Api.DeleteComplexFormComponent(entry!.Components.Single());

        entry = await Api.GetEntry(_complexFormEntryId);
        entry.Should().NotBeNull();
        entry.Components.Should().BeEmpty();
    }

    private async Task AddDuplicateComponent()
    {
        var fwDataApi = (FwDataMiniLcmApi)BaseApi;
        var complexFormEntry = fwDataApi.EntriesRepository.GetObject(_complexFormEntryId);
        var componentEntry = fwDataApi.EntriesRepository.GetObject(_componentEntryId);
        await fwDataApi.Cache.DoUsingNewOrCurrentUOW("Add ComplexFormEntryRef",
            "Remove ComplexFormEntryRef",
            () =>
            {
                complexFormEntry.EntryRefsOS[0].ComponentLexemesRS.Add(componentEntry);
                complexFormEntry.EntryRefsOS[1].ComponentLexemesRS.Add(componentEntry);
                return ValueTask.CompletedTask;
            });
    }

    [Fact]
    public async Task DuplicateComplexFormTypes_AreNotDuplicated()
    {
        await AddDuplicateFormType();

        var entry = await Api.GetEntry(_complexFormEntryId);
        entry.Should().NotBeNull();
        entry.ComplexFormTypes.Should().HaveCount(1);
    }

    [Fact]
    public async Task DuplicateComplexFormTypes_BothAreRemoved()
    {
        await AddDuplicateFormType();

        var entry = await Api.GetEntry(_complexFormEntryId);
        await Api.RemoveComplexFormType(_complexFormEntryId, entry!.ComplexFormTypes.Single().Id);

        entry = await Api.GetEntry(_complexFormEntryId);
        entry.Should().NotBeNull();
        entry.ComplexFormTypes.Should().BeEmpty();
    }

    private async Task AddDuplicateFormType()
    {
        var fwDataApi = (FwDataMiniLcmApi)BaseApi;
        var complexFormEntry = fwDataApi.EntriesRepository.GetObject(_complexFormEntryId);
        var complexFormType = fwDataApi.ComplexFormTypesFlattened.First();
        await fwDataApi.Cache.DoUsingNewOrCurrentUOW("Add ComplexFormEntryRef",
            "Remove ComplexFormEntryRef",
            () =>
            {
                complexFormEntry.EntryRefsOS[0].ComplexEntryTypesRS.Add(complexFormType);
                complexFormEntry.EntryRefsOS[1].ComplexEntryTypesRS.Add(complexFormType);
                return ValueTask.CompletedTask;
            });
    }
}
