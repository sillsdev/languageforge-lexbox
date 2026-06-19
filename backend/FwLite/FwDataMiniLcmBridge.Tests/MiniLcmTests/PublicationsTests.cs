using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;
using SIL.LCModel.Infrastructure;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class PublicationsTests(ProjectLoaderFixture fixture) : PublicationsTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("publications-test", "en", "en"));
    }

    // FieldWorks' main publication is its protected one; the MiniLcm write path deliberately never flips
    // IsProtected (see UpdatePublicationProxy), so seed it directly at the LCM layer for these tests.
    protected override async Task<Publication> SeedMainPublication(string name = "Main")
    {
        var fwApi = (FwDataMiniLcmApi)BaseApi;
        var pub = await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", name } } });
        var lcmPublication = fwApi.GetLcmPublication(pub.Id)
            ?? throw new InvalidOperationException($"Seeded publication {pub.Id} not found in LCM");
        NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(fwApi.Cache.ServiceLocator.ActionHandler,
            () => lcmPublication.IsProtected = true);
        return await Api.GetPublication(pub.Id) ?? throw new InvalidOperationException($"Seeded main {pub.Id} not found");
    }
}
