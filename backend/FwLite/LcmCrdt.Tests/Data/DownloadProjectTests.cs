using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LcmCrdt.Tests.Data;

public class DownloadProjectTests : IAsyncLifetime
{
    private readonly RegressionTestHelper _helper = new("DownloadProject");
    private readonly MiniLcmApiFixture _apiFixture = MiniLcmApiFixture.Create(false);

    public async Task InitializeAsync()
    {
        await _helper.InitializeAsync();
        await _apiFixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _helper.DisposeAsync();
        await _apiFixture.DisposeAsync();
    }

    [Fact]
    public async Task CanCreateANewProjectViaSync()
    {
        var remoteModel = _helper.Services.GetRequiredService<DataModel>();
        await _apiFixture.DataModel.SyncWith(remoteModel);
    }
}
