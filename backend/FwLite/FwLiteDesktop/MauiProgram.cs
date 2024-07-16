using Microsoft.Extensions.Logging;

namespace FwLiteDesktop;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });


        builder.Services.AddOptions<LocalWebAppConfig>().Configure(config =>
            {
                config.Url = "http://localhost:5000";
            }
        );

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
