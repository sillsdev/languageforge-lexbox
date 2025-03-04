using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LcmCrdt.Tests.Data;

public class MigrationTests: IAsyncLifetime
{
    private IHost _host = null!;
    private AsyncServiceScope _asyncScope;


    public Task InitializeAsync()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        _host = builder.Build();
        var services = _host.Services;
        _asyncScope = services.CreateAsyncScope();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _asyncScope.DisposeAsync();
        if (_host is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync();
        else
        {
            _host.Dispose();
        }
    }

    private async Task InitDbFromScripts()
    {
        var initialSqlFile = GetFilePath("Scripts/v1.sql");
        var projectsService = _asyncScope.ServiceProvider.GetRequiredService<CurrentProjectService>();
        var crdtProject = new CrdtProject("MigrationTest", "MigrationTest.sqlite");
        if (File.Exists(crdtProject.DbPath)) File.Delete(crdtProject.DbPath);
        projectsService.SetupProjectContextForNewDb(crdtProject);
        var lcmCrdtDbContext = _asyncScope.ServiceProvider.GetRequiredService<LcmCrdtDbContext>();
        var sql = await File.ReadAllTextAsync(initialSqlFile);
        var dbConnection = lcmCrdtDbContext.Database.GetDbConnection();
        dbConnection.Open();
        var dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = sql;
        await dbCommand.ExecuteNonQueryAsync();
        //need to close the connection, otherwise the collations won't get created, they would normally be created on open or save, so we're closing so they get created when EF opens the connection.
        dbConnection.Close();

        await lcmCrdtDbContext.Database.MigrateAsync();

        await projectsService.RefreshProjectData();
    }

    [Fact]
    public async Task GetEntries_WorksAfterMigrationFromScriptedDb()
    {
        await InitDbFromScripts();
        var api = _asyncScope.ServiceProvider.GetRequiredService<IMiniLcmApi>();
        var hasEntries = false;
        await foreach (var entry in api.GetEntries(new(Count: 100)))
        {
            hasEntries = true;
            entry.Should().NotBeNull();
        }

        hasEntries.Should().BeTrue();
    }

    [Fact]
    public async Task CanReadEntriesAfterMigrating()
    {
        var originalPath = GetFilePath("sena-3.sqlite");
        var dbPath = GetFilePath("sena-3-for-migrating.sqlite");
        File.Copy(originalPath, dbPath, true);
        var sena3Project = new CrdtProject("sena-3", dbPath);
        var miniLcmApi = await _asyncScope.ServiceProvider.OpenCrdtProject(sena3Project);

        await _asyncScope.ServiceProvider.GetRequiredService<LcmCrdtDbContext>().Database.MigrateAsync();

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
