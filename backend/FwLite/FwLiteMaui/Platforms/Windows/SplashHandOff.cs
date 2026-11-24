using System.Runtime.InteropServices;
using FwLiteShared.Services;
using Microsoft.Maui.Platform;
using static FwLiteMaui.WinUI.NativeMethods;

namespace FwLiteMaui.WinUI;

/// <summary>
/// Keeps the first main window off screen until the viewer has loaded, then swaps it in for the
/// <see cref="StartupSplash"/>, so the splash is the only loading state the user sees.
/// <para>
/// Whatever fails here, the app must still open: the window comes back on screen and the splash closes.
/// </para>
/// </summary>
internal static class SplashHandOff
{
    /// <summary>If the viewer never reports in, show the window anyway so its error is visible.</summary>
    private static readonly TimeSpan RevealTimeout = TimeSpan.FromSeconds(20);
    private static bool _started;

    public static void HoldUntilViewerLoaded(Microsoft.UI.Xaml.Window window, ViewerLoadState? viewerLoadState)
    {
        if (_started) return;
        _started = true;
        var held = false;
        try
        {
            held = viewerLoadState is not null && TryHold(window.GetWindowHandle(), viewerLoadState);
            if (held) window.Activated += RemoveSplashFromTaskbar;
        }
        catch
        {
            //shown the normal way, without the hand-off
        }
        if (!held) StartupSplash.Close();
    }

    private static void RemoveSplashFromTaskbar(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        ((Microsoft.UI.Xaml.Window)sender).Activated -= RemoveSplashFromTaskbar;
        StartupSplash.RemoveFromTaskbar();
    }

    private static bool TryHold(nint hwnd, ViewerLoadState viewerLoadState)
    {
        if (!StartupSplash.WaitUntilShowing() || !GetWindowRect(hwnd, out var onScreen)) return false;
        StartupSplash.MoveToMonitorOf(hwnd);
        var belowScreens = GetSystemMetrics(SM_YVIRTUALSCREEN) + GetSystemMetrics(SM_CYVIRTUALSCREEN) + 100;
        //off screen, not hidden: the page loads and paints there, so the window arrives finished
        if (!MoveTo(hwnd, onScreen.Left, belowScreens)) return false;
        _ = RevealWhenLoaded();
        return true;

        async Task RevealWhenLoaded()
        {
            try
            {
                await Task.WhenAny(viewerLoadState.Finished, Task.Delay(RevealTimeout));
            }
            finally
            {
                if (!MoveNormalPosition(hwnd, onScreen.Top - belowScreens)) MoveTo(hwnd, onScreen.Left, onScreen.Top);
                StartupSplash.Close();
            }
        }
    }

    private static bool MoveTo(nint hwnd, int x, int y)
    {
        return SetWindowPos(hwnd, 0, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    /// <summary>Unlike SetWindowPos, this also moves a window that was minimized or maximized while it waited.</summary>
    private static bool MoveNormalPosition(nint hwnd, int down)
    {
        var placement = new WINDOWPLACEMENT { length = Marshal.SizeOf<WINDOWPLACEMENT>() };
        if (!GetWindowPlacement(hwnd, ref placement)) return false;
        placement.rcNormalPosition.Top += down;
        placement.rcNormalPosition.Bottom += down;
        return SetWindowPlacement(hwnd, ref placement);
    }
}
