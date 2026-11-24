using System.Runtime.InteropServices;

namespace FwLiteMaui.WinUI;

/// <summary>Win32 declarations for <see cref="StartupSplash"/> and <see cref="SplashHandOff"/>.</summary>
internal static class NativeMethods
{
    internal const uint WM_DESTROY = 0x0002, WM_PAINT = 0x000F, WM_CLOSE = 0x0010, WM_ERASEBKGND = 0x0014,
        WM_NCHITTEST = 0x0084, WM_TIMER = 0x0113, WM_DPICHANGED = 0x02E0;
    internal const uint WS_POPUP = 0x80000000, WS_EX_APPWINDOW = 0x00040000;
    internal const uint SWP_NOSIZE = 0x0001, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010;
    internal const uint MONITOR_DEFAULTTOPRIMARY = 1, MONITOR_DEFAULTTONEAREST = 2;
    internal const uint AW_BLEND = 0x00080000, AW_HIDE = 0x00010000, SRCCOPY = 0x00CC0020;
    internal const uint DT_CENTER = 0x1, DT_VCENTER = 0x4, DT_SINGLELINE = 0x20, DT_NOPREFIX = 0x800, DT_END_ELLIPSIS = 0x8000;
    internal const int SM_YVIRTUALSCREEN = 77, SM_CYVIRTUALSCREEN = 79;
    internal const int SW_SHOW = 5, HTCAPTION = 2, TRANSPARENT = 1, IDC_ARROW = 32512, CLEARTYPE_QUALITY = 5;
    internal const int DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWCP_ROUND = 2;
    internal const int InterpolationModeHighQualityBicubic = 7;

    [ComImport, Guid("56FDF342-FD6D-11d0-958A-006097C9A090"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITaskbarList
    {
        void HrInit();
        void AddTab(nint hwnd);
        void DeleteTab(nint hwnd);
    }

    [ComImport, Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
    internal class TaskbarList;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate nint WndProcDelegate(nint hwnd, uint msg, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WNDCLASSEXW
    {
        public int cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra, cbWndExtra;
        public nint hInstance, hIcon, hCursor, hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWPLACEMENT
    {
        public int length;
        public uint flags, showCmd;
        public int minX, minY, maxX, maxY;
        public RECT rcNormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor, rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam, lParam;
        public uint time;
        public int x, y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PAINTSTRUCT
    {
        public nint hdc;
        public int fErase;
        public RECT rcPaint;
        public int fRestore, fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GdiplusStartupInput
    {
        public uint GdiplusVersion;
        public nint DebugEventCallback;
        public int SuppressBackgroundThread, SuppressExternalCodecs;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern nint GetModuleHandleW(string? name);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern ushort RegisterClassExW(ref WNDCLASSEXW windowClass);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern nint CreateWindowExW(uint exStyle, string className, string title, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);
    [DllImport("user32.dll")] internal static extern nint DefWindowProcW(nint hwnd, uint msg, nint wParam, nint lParam);
    [DllImport("user32.dll")] internal static extern bool DestroyWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(nint hwnd, int command);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(nint hwnd, out RECT rect);
    [DllImport("user32.dll")] internal static extern bool GetWindowPlacement(nint hwnd, ref WINDOWPLACEMENT placement);
    [DllImport("user32.dll")] internal static extern bool SetWindowPlacement(nint hwnd, ref WINDOWPLACEMENT placement);
    [DllImport("user32.dll")] internal static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] internal static extern bool AnimateWindow(nint hwnd, uint milliseconds, uint flags);
    [DllImport("user32.dll")] internal static extern int GetMessageW(out MSG msg, nint hwnd, uint min, uint max);
    [DllImport("user32.dll")] internal static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32.dll")] internal static extern nint DispatchMessageW(ref MSG msg);
    [DllImport("user32.dll")] internal static extern bool PostMessageW(nint hwnd, uint msg, nint wParam, nint lParam);
    [DllImport("user32.dll")] internal static extern void PostQuitMessage(int exitCode);
    [DllImport("user32.dll")] internal static extern nuint SetTimer(nint hwnd, nuint id, uint milliseconds, nint callback);
    [DllImport("user32.dll")] internal static extern nint LoadCursorW(nint instance, nint cursor);
    [DllImport("user32.dll")] internal static extern int GetDpiForWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern nint MonitorFromWindow(nint hwnd, uint flags);
    [DllImport("user32.dll")] internal static extern bool GetMonitorInfoW(nint monitor, ref MONITORINFO info);
    [DllImport("user32.dll")] internal static extern bool GetClientRect(nint hwnd, out RECT rect);
    [DllImport("user32.dll")] internal static extern bool InvalidateRect(nint hwnd, ref RECT rect, bool erase);
    [DllImport("user32.dll")] internal static extern bool InvalidateRect(nint hwnd, nint rect, bool erase);
    [DllImport("user32.dll")] internal static extern nint BeginPaint(nint hwnd, out PAINTSTRUCT paint);
    [DllImport("user32.dll")] internal static extern bool EndPaint(nint hwnd, ref PAINTSTRUCT paint);
    [DllImport("user32.dll")] internal static extern int FillRect(nint hdc, ref RECT rect, nint brush);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int DrawTextW(nint hdc, string text, int length, ref RECT rect, uint format);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] internal static extern nint ExtractIconW(nint instance, string exePath, uint index);
    [DllImport("gdi32.dll")] internal static extern nint CreateCompatibleDC(nint hdc);
    [DllImport("gdi32.dll")] internal static extern nint CreateCompatibleBitmap(nint hdc, int width, int height);
    [DllImport("gdi32.dll")] internal static extern nint SelectObject(nint hdc, nint obj);
    [DllImport("gdi32.dll")] internal static extern bool DeleteObject(nint obj);
    [DllImport("gdi32.dll")] internal static extern bool DeleteDC(nint hdc);
    [DllImport("gdi32.dll")] internal static extern bool BitBlt(nint dest, int x, int y, int width, int height, nint source, int sourceX, int sourceY, uint operation);
    [DllImport("gdi32.dll")] internal static extern nint CreateSolidBrush(int color);
    [DllImport("gdi32.dll")] internal static extern int SetBkMode(nint hdc, int mode);
    [DllImport("gdi32.dll")] internal static extern int SetTextColor(nint hdc, int color);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] internal static extern nint CreateFontW(int height, int width, int escapement, int orientation, int weight, uint italic, uint underline, uint strikeOut, uint charSet, uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string face);
    [DllImport("dwmapi.dll")] internal static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);
    [DllImport("gdiplus.dll")] internal static extern int GdiplusStartup(out nint token, ref GdiplusStartupInput input, nint output);
    [DllImport("gdiplus.dll")] internal static extern int GdipLoadImageFromStream(nint stream, out nint image);
    [DllImport("shlwapi.dll")] internal static extern nint SHCreateMemStream(byte[] data, int length);
    [DllImport("gdiplus.dll")] internal static extern int GdipCreateFromHDC(nint hdc, out nint graphics);
    [DllImport("gdiplus.dll")] internal static extern int GdipSetInterpolationMode(nint graphics, int mode);
    [DllImport("gdiplus.dll")] internal static extern int GdipDrawImageRectI(nint graphics, nint image, int x, int y, int width, int height);
    [DllImport("gdiplus.dll")] internal static extern int GdipDeleteGraphics(nint graphics);
}
