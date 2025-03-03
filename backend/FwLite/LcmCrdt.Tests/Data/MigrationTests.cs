using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LcmCrdt.Tests.Data;

public class MigrationTests
{

    [Fact]
    public async Task CanReadEntriesAfterMigrating()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        using var host = builder.Build();
        var services = host.Services;
        var asyncScope = services.CreateAsyncScope();

        var originalPath = GetFilePath("sena-3.sqlite");
        var dbPath = GetFilePath("sena-3-for-migrating.sqlite");
        File.Copy(originalPath, dbPath, true);
        var sena3Project = new CrdtProject("sena-3", dbPath);
        var miniLcmApi = await asyncScope.ServiceProvider.OpenCrdtProject(sena3Project);

        await asyncScope.ServiceProvider.GetRequiredService<LcmCrdtDbContext>().Database.MigrateAsync();

        await foreach (var entry in miniLcmApi.GetEntries(new(Count: 100)))
        {
            entry.Should().NotBeNull();
        }
    }

    private static string GetFilePath(string name, [CallerFilePath] string sourceFile = "")
    {
        return Path.Combine(
            Path.GetDirectoryName(sourceFile) ?? throw new InvalidOperationException("Could not get directory of source file"),
            name);
    }
}
