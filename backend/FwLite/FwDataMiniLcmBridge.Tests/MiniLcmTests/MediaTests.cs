using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class MediaTests : MediaTestsBase
{
    private readonly ProjectLoaderFixture _fixture;

    public MediaTests(ProjectLoaderFixture fixture)
    {
        _fixture = fixture;
    }

    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(_fixture.NewProjectApi("media-test", "en", "en"));
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var projectFolder = ((FwDataMiniLcmApi)BaseApi).Cache.LangProject.LinkedFilesRootDir;
        Directory.CreateDirectory(projectFolder);
    }

    public override async Task DisposeAsync()
    {
        var projectFolder = ((FwDataMiniLcmApi)BaseApi).Cache.ProjectId.ProjectFolder;
        if (Directory.Exists(projectFolder)) Directory.Delete(projectFolder, true);
        await base.DisposeAsync();
    }
}
