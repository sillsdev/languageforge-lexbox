using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LcmCrdt.Tests.Data;

public class MigrationTests: IAsyncLifetime
{
    private readonly RegressionTestHelper _helper = new("MigrationTest");

    public async Task InitializeAsync()
    {
        await _helper.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _helper.DisposeAsync();
    }

    [Fact]
    public async Task GetEntries_WorksAfterMigrationFromScriptedDb()
    {
        //manually open the connection, this prevents a race condition where the entries query runs before the collations are created.
        await _helper.Services.GetRequiredService<LcmCrdtDbContext>().Database.OpenConnectionAsync();
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
