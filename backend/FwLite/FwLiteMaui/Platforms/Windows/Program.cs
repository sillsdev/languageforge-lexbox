using Microsoft.UI.Dispatching;

namespace FwLiteMaui.WinUI;

/// <summary>
/// Replaces the Main that XAML generates (DISABLE_XAML_GENERATED_MAIN) so the splash is up before WinUI loads.
/// </summary>
public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        StartupSplash.Show(args);
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Microsoft.UI.Xaml.Application.Start(_ =>
        {
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            new App();
        });
    }
}
