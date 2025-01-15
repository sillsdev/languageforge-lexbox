using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LcmCrdt.Data;

[Obsolete("this class is only here to facilitate migrations, it's not used in the application", true)]
public class DbContextDesignFactory: IDesignTimeDbContextFactory<LcmCrdtDbContext>
{
    public LcmCrdtDbContext CreateDbContext(string[] args)
    {
        var servicesRoot = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationRoot([]))
            .AddLcmCrdtClient()
            .BuildServiceProvider();
        var scope = servicesRoot.CreateScope();
        scope.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContextForNewDb(new ("DesignDb", "design.sqlite"));
        return scope.ServiceProvider.GetRequiredService<LcmCrdtDbContext>();
    }
}
