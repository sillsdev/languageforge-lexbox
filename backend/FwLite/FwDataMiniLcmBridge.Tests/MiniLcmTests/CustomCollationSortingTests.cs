using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;
using SIL.WritingSystems;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class CustomCollationSortingTests(ProjectLoaderFixture fixture) : CustomCollationSortingTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("collation-sorting-test", "en", "en"));
    }

    protected override async Task CreateWritingSystemWithCollation(WritingSystem writingSystem)
    {
        await Api.CreateWritingSystem(writingSystem with { IcuCollationRules = null, SystemCollationLocale = null });
        var fwDataApi = (FwDataMiniLcmApi)BaseApi;
        await fwDataApi.Cache.DoUsingNewOrCurrentUOW("Set collation",
            "Revert collation",
            () =>
            {
                // collation is import-only, so it can't be set through the api
                var ws = fwDataApi.Cache.ServiceLocator.WritingSystemManager.Get(writingSystem.WsId.Code);
                ws.DefaultCollation = writingSystem.IcuCollationRules is { } rules
                    ? new IcuRulesCollationDefinition("standard") { IcuRules = rules }
                    : new SystemCollationDefinition { LanguageTag = writingSystem.SystemCollationLocale };
                return ValueTask.CompletedTask;
            });
    }
}
