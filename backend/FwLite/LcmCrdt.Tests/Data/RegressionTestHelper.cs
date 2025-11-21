using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LcmCrdt.Tests.Data;

public class RegressionTestHelper(string dbName): IAsyncLifetime
{
    private IHost _host = null!;
    private AsyncServiceScope _asyncScope;
    public IServiceProvider Services => _asyncScope.ServiceProvider;

    private async Task InitDbFromScripts(RegressionVersion version)
    {
        var initialSqlFile = GetFilePath($"Scripts/{version}.sql");
        var projectsService = _asyncScope.ServiceProvider.GetRequiredService<CurrentProjectService>();
        var crdtProject = new CrdtProject(dbName, $"{dbName}.sqlite");
        if (File.Exists(crdtProject.DbPath)) File.Delete(crdtProject.DbPath);
        projectsService.SetupProjectContextForNewDb(crdtProject);
        await using var lcmCrdtDbContext = await _asyncScope.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        var sql = await File.ReadAllTextAsync(initialSqlFile);
        var dbConnection = lcmCrdtDbContext.Database.GetDbConnection();
        await dbConnection.OpenAsync();
        var dbCommand = dbConnection.CreateCommand();
        // Make virtual tables immediately useable (included here so we don't have to update future sql dumps)
        // see: https://sqlite.org/forum/info/b68bc59ddd4aeca38d44823611fa931a671158403e4af522105bf2ba21a96327
        var resetSchema = "PRAGMA writable_schema=RESET;";
        dbCommand.CommandText = $"{sql}\n{resetSchema}";
        await dbCommand.ExecuteNonQueryAsync();

        //need to close the connection, otherwise the collations won't get created, they would normally be created on open or save, so we're closing so they get created when EF opens the connection.
        await dbConnection.CloseAsync();

        await lcmCrdtDbContext.Database.MigrateAsync();

        await projectsService.RefreshProjectData();
    }

    public Task InitializeAsync()
    {
        return InitializeAsync(RegressionVersion.v2);
    }

    public async Task InitializeAsync(RegressionVersion version)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddTestLcmCrdtClient();
        _host = builder.Build();
        var services = _host.Services;
        _asyncScope = services.CreateAsyncScope();
        await InitDbFromScripts(version);
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

    private static string GetFilePath(string name, [CallerFilePath] string sourceFile = "")
    {
        return Path.Combine(
            Path.GetDirectoryName(sourceFile) ??
            throw new InvalidOperationException("Could not get directory of source file"),
            name);
    }

    public enum RegressionVersion
    {
        v1,
        v2
    }
}
