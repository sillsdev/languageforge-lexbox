
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
    //windows is dumb and so you can't send SIGINT to a process, so we need to listen for a shutdown command
    _ = Task.Run(async () =>
         {
             // Wait for the "shutdown" command from stdin
             while ((await Console.In.ReadLineAsync())?.Trim() is not ("shutdown" or null)) { }

             await app.StopAsync();
         });

    await app.WaitForShutdownAsync();
}
