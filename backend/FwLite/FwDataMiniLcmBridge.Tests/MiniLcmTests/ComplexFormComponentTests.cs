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
        var fwDataApi = (FwDataMiniLcmApi)Api;
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
}
