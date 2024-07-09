namespace LocalWebApp.Utils;

public static class ConfigExtensions
{
    public static void ConfigureProd<T>(this WebApplicationBuilder builder, Action<T> configureOptions) where T : class
    {
        if (!builder.Environment.IsProduction()) return;
        builder.Services.Configure(configureOptions);
    }

    public static void ConfigureDev<T>(this WebApplicationBuilder builder, Action<T> configureOptions) where T : class
    {
        if (!builder.Environment.IsDevelopment()) return;
        builder.Services.Configure(configureOptions);
    }
}
