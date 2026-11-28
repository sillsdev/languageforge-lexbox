using ObjCRuntime;
using UIKit;
using Velopack;

namespace FwLiteMaui;
public class Program
{
    // This is the main entry point of the application.
    static void Main(string[] args)
    {
        // Velopack must run first: it handles its own install/update hooks (and may exit) and applies an
        // update that was downloaded but not applied before the last quit. A failure here, e.g. a dev build
        // that isn't in a Velopack-packed bundle, must never stop the app from starting.
        try
        {
            VelopackApp.Build().SetArgs(args).Run();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Velopack startup failed, continuing without auto-update: {e}");
        }

        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
