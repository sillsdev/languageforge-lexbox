
using FwLiteWeb;
using Microsoft.Extensions.Options;

//paratext won't let us change the working directory, and if it's not set correctly then loading js files doesn't work
Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(Program).Assembly.Location)!);
var app = FwLiteWebServer.SetupAppServer(new() {Args = args});
await using (app)
{
    await app.StartAsync();
    var openBrowser = app.Services.GetRequiredService<IOptions<FwLiteWebConfig>>().Value.OpenBrowser;

    if (openBrowser)
    {
        var url = app.Urls.First();
        LocalAppLauncher.LaunchBrowser(url);
    }
    // Windows can't receive SIGINT as a child; listen for a "shutdown" command on stdin.
    _ = Task.Run(async () =>
    {
        string? line;
        while ((line = await Console.In.ReadLineAsync()) is not null
               && !string.Equals(line.Trim(), "shutdown", StringComparison.OrdinalIgnoreCase))
        { /* keep waiting */ }

        await app.StopAsync();
    });

    await app.WaitForShutdownAsync();
}
