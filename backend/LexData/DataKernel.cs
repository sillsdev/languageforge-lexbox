using LexData.Configuration;
using LexData.EntityIds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LexData;

public static class DataKernel
{
    public static void AddLexData(this IServiceCollection services)
    {
        services.AddDbContext<LexBoxDbContext>((serviceProvider, options) =>
        {
            options.EnableDetailedErrors();
            options.UseNpgsql(serviceProvider.GetRequiredService<IOptions<DbConfig>>().Value.LexBoxConnectionString);
            options.UseLfIdConverters();
        });
        services.AddHostedService<DbStartupService>();
        services.AddOptions<DbConfig>()
            .BindConfiguration("DbConfig")
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}