using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LcmCrdt.Tests.Data;

public class MigrationTests: IAsyncLifetime
{
    private readonly RegressionTestHelper _helper = new("MigrationTest");

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _helper.DisposeAsync();
    }

    [Theory]
    [InlineData(RegressionTestHelper.RegressionVersion.v1)]
    [InlineData(RegressionTestHelper.RegressionVersion.v2)]
    public async Task GetEntries_WorksAfterMigrationFromScriptedDb(RegressionTestHelper.RegressionVersion regressionVersion)
    {
        await _helper.InitializeAsync(regressionVersion);
        var api = _helper.Services.GetRequiredService<IMiniLcmApi>();
        var hasEntries = false;
        await foreach (var entry in api.GetEntries(new(Count: 100)))
        {
            hasEntries = true;
            entry.Should().NotBeNull();
        }

        hasEntries.Should().BeTrue();
    }
}
