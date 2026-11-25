using Microsoft.UI.Dispatching;

namespace FwLiteMaui.WinUI;

/// <summary>
/// Replaces the Main that XAML generates (DISABLE_XAML_GENERATED_MAIN) so the splash is up before WinUI loads.
/// The rest is a copy of the generated one (obj/**/Platforms/Windows/App.g.i.cs): compare them when updating the Windows App SDK.
/// </summary>
public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            StartupSplash.Show(args);
        }
        catch
        {
            //the splash is a nicety, startup carries on without it
        }
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Microsoft.UI.Xaml.Application.Start(_ =>
        {
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            new App();
        });
    }
}
