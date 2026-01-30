
using FwLiteWeb;
using Microsoft.Extensions.Options;

//paratext won't let us change the working directory, and if it's not set correctly then loading js files doesn't work
var assemblyLocation = typeof(Program).Assembly.Location;
var assemblyDirectory = !string.IsNullOrEmpty(assemblyLocation)
    ? Path.GetDirectoryName(assemblyLocation) : null;
var appDirectory = assemblyDirectory ?? AppContext.BaseDirectory;
Directory.SetCurrentDirectory(appDirectory);

var app = FwLiteWebServer.SetupAppServer(new() { Args = args });
await using (app)
{
    await app.StartAsync();
    var openBrowser = app.Services.GetRequiredService<IOptions<FwLiteWebConfig>>().Value.OpenBrowser;

    if (openBrowser)
    {
        var url = app.Urls.First();
        LocalAppLauncher.LaunchBrowser(url);
    }
    // Windows doesn't allow sending SIGINT to a process, so we need to listen for a shutdown command
    _ = Task.Run(async () =>
         {
             // Wait for the "shutdown" command from stdin
             while ((await Console.In.ReadLineAsync())?.Trim() is not ("shutdown" or null)) { }

             await app.StopAsync();
         });

    _ = Task.Run(async () =>
    {
        // Wait for the "shutdown" command from stdin
        while (await Console.In.ReadLineAsync() is not "shutdown") { }

        await app.StopAsync();
    });

    await app.WaitForShutdownAsync();
}
