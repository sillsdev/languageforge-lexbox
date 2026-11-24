using System.Runtime.InteropServices;
using FwLiteShared.Services;
using Microsoft.Maui.Platform;

namespace FwLiteMaui.WinUI;

/// <summary>
/// Keeps the first main window off screen until the viewer has loaded, then swaps it in for the
/// <see cref="StartupSplash"/>, so the splash is the only loading state the user sees.
/// </summary>
internal static class SplashHandOff
{
    /// <summary>If the viewer never reports in, show the window anyway so its error is visible.</summary>
    private static readonly TimeSpan RevealTimeout = TimeSpan.FromSeconds(20);
    private static bool _started;

    public static void HoldUntilViewerLoaded(Microsoft.UI.Xaml.Window window, ViewerLoadState viewerLoadState)
    {
        if (_started || !StartupSplash.IsShowing) return;
        _started = true;
        var hwnd = window.GetWindowHandle();
        //off screen, not hidden: the page loads and paints there, so the window arrives finished
        StartupSplash.MoveToMonitorOf(hwnd);
        if (!GetWindowRect(hwnd, out var placement) || !MoveTo(hwnd, placement.Left, OffScreenTop()))
        {
            StartupSplash.Close();
            return;
        }
        _ = RevealWhenLoaded();

        async Task RevealWhenLoaded()
        {
            await Task.WhenAny(viewerLoadState.Finished, Task.Delay(RevealTimeout));
            MoveTo(hwnd, placement.Left, placement.Top);
            StartupSplash.Close();
        }
    }

    private static int OffScreenTop()
    {
        const int SM_YVIRTUALSCREEN = 77, SM_CYVIRTUALSCREEN = 79;
        return GetSystemMetrics(SM_YVIRTUALSCREEN) + GetSystemMetrics(SM_CYVIRTUALSCREEN) + 100;
    }

    private static bool MoveTo(nint hwnd, int x, int y)
    {
        const uint SWP_NOSIZE = 0x0001, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010;
        return SetWindowPos(hwnd, 0, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hwnd, out RECT rect);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
}
