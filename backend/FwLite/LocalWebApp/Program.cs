
using LocalWebApp;

var app = LocalWebAppServer.SetupAppServer(new() {Args = args});
await using (app)
{
    await app.StartAsync();

    if (!app.Environment.IsDevelopment())
    {
        var url = app.Urls.First();
        LocalAppLauncher.LaunchBrowser(url);
    }

    await app.WaitForShutdownAsync();
}
