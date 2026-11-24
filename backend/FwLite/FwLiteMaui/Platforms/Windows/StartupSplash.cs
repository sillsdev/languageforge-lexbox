using System.Runtime.InteropServices;
using static FwLiteMaui.WinUI.NativeMethods;

namespace FwLiteMaui.WinUI;

/// <summary>
/// A small native window shown from the first line of Main until the viewer has loaded (see
/// <see cref="SplashHandOff"/>), so a slow start (cold disk) or the one-time data migration isn't a wait with
/// nothing on screen.
/// <para>
/// Plain Win32 on its own thread: it has to be up before WinUI and MAUI load, and keep painting while the UI
/// thread builds the host.
/// </para>
/// </summary>
internal static class StartupSplash
{
    private const string ClassName = "FwLiteStartupSplash";
    private const string LogoResource = "splash-logo.png";
    private const int WidthDip = 380, HeightDip = 240, LogoDip = 96;
    //0xBBGGRR; the background is the MauiSplashScreen colour in FwLiteMaui.csproj
    private const int BackgroundColor = 0x442a1f, TrackColor = 0x69554c, StatusColor = 0xe6d6c8, White = 0xffffff;
    private static readonly WndProcDelegate WndProcKeepAlive = WndProc;
    private static readonly ManualResetEventSlim WindowCreated = new();
    private static volatile nint _hwnd;
    private static volatile bool _closeRequested;
    private static int _dpi = 96;
    private static nint _logo, _titleFont, _statusFont;
    private static volatile string _status = "";
    private static bool _requested;

    public static void Show(string[] args)
    {
        //a toast click starts the process as a COM server, which may never open a window
        if (args.Contains("-ToastActivated")) return;
        _requested = true;
        new Thread(Run) { IsBackground = true, Name = "Startup splash" }.Start();
    }

    /// <summary>False when there is no splash: never requested, failed, or closed already.</summary>
    public static bool WaitUntilShowing()
    {
        if (!_requested) return false;
        //the splash thread may still be creating the window
        WindowCreated.Wait(TimeSpan.FromSeconds(1));
        return _hwnd != 0;
    }

    public static void SetStatus(string status)
    {
        _status = status;
        if (WaitUntilShowing()) InvalidateRect(_hwnd, 0, false);
    }

    /// <summary>Windows opens the main window on the monitor the app was launched from, which may not be the primary one.</summary>
    public static void MoveToMonitorOf(nint window)
    {
        var splash = _hwnd;
        if (splash == 0) return;
        var monitor = MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST);
        if (monitor != MonitorFromWindow(splash, MONITOR_DEFAULTTONEAREST)) CenterOn(splash, monitor);
    }

    /// <summary>Once the main window has a taskbar button, so the app doesn't show as two windows there.</summary>
    public static void RemoveFromTaskbar()
    {
        var splash = _hwnd;
        if (splash == 0) return;
        try
        {
            var taskbar = (ITaskbarList)new TaskbarList();
            taskbar.HrInit();
            taskbar.DeleteTab(splash);
        }
        catch
        {
            //two taskbar buttons for a moment is all that's lost
        }
    }

    public static void Close()
    {
        //the flag covers a close that comes before the splash thread has made the window
        _closeRequested = true;
        var splash = _hwnd;
        if (splash != 0) PostMessageW(splash, WM_CLOSE, 0, 0);
    }

    private static void Run()
    {
        try
        {
            var hInstance = GetModuleHandleW(null);
            var windowClass = new WNDCLASSEXW
            {
                cbSize = Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WndProcKeepAlive),
                hInstance = hInstance,
                hCursor = LoadCursorW(0, IDC_ARROW),
                hIcon = ExtractIconW(hInstance, Environment.ProcessPath ?? "", 0),
                lpszClassName = ClassName,
            };
            if (RegisterClassExW(ref windowClass) == 0) return;
            //WS_EX_APPWINDOW: a taskbar button, until the main window has one (RemoveFromTaskbar)
            var hwnd = CreateWindowExW(WS_EX_APPWINDOW, ClassName, "FieldWorks Lite", WS_POPUP, 0, 0, 0, 0, 0, 0, hInstance, 0);
            if (hwnd == 0) return;

            _dpi = GetDpiForWindow(hwnd);
            CenterOn(hwnd, MonitorFromWindow(hwnd, MONITOR_DEFAULTTOPRIMARY));
            //rounded corners on Windows 11, ignored on Windows 10
            var round = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));

            LoadLogo();
            CreateFonts();

            _hwnd = hwnd;
            WindowCreated.Set();
            if (_closeRequested) return;
            ShowWindow(hwnd, SW_SHOW);
            SetTimer(hwnd, 1, 16, 0);
            while (GetMessageW(out var msg, 0, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessageW(ref msg);
            }
        }
        catch
        {
            //the splash is a nicety, startup carries on without it
        }
        finally
        {
            _hwnd = 0;
            WindowCreated.Set();
        }
    }

    private static int Px(int dip) => dip * _dpi / 96;

    private static void CenterOn(nint hwnd, nint monitor)
    {
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfoW(monitor, ref info)) return;
        var area = info.rcWork;
        int width = Px(WidthDip), height = Px(HeightDip);
        SetWindowPos(hwnd, 0,
            area.Left + (area.Right - area.Left - width) / 2,
            area.Top + (area.Bottom - area.Top - height) / 2,
            width, height, SWP_NOZORDER | SWP_NOACTIVATE);
    }

    private static void CreateFonts()
    {
        if (_titleFont != 0) DeleteObject(_titleFont);
        if (_statusFont != 0) DeleteObject(_statusFont);
        _titleFont = CreateFontW(-Px(20), 0, 0, 0, 600, 0, 0, 0, 1, 0, 0, CLEARTYPE_QUALITY, 0, "Segoe UI");
        _statusFont = CreateFontW(-Px(13), 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, CLEARTYPE_QUALITY, 0, "Segoe UI");
    }

    private static void LoadLogo()
    {
        using var resource = typeof(StartupSplash).Assembly.GetManifestResourceStream(LogoResource);
        if (resource is null) return;
        var png = new byte[resource.Length];
        resource.ReadExactly(png);
        var startupInput = new GdiplusStartupInput { GdiplusVersion = 1 };
        if (GdiplusStartup(out _, ref startupInput, 0) != 0) return;
        var stream = SHCreateMemStream(png, png.Length);
        if (stream == 0) return;
        if (GdipLoadImageFromStream(stream, out var image) == 0) _logo = image;
        Marshal.Release(stream);
    }

    private static nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        try
        {
            return HandleMessage(hwnd, msg, wParam, lParam);
        }
        catch
        {
            //an exception that escapes into Windows ends the process; lose the splash instead
            DestroyWindow(hwnd);
            return 0;
        }
    }

    private static nint HandleMessage(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case WM_PAINT:
                var hdc = BeginPaint(hwnd, out var paint);
                Paint(hwnd, hdc);
                EndPaint(hwnd, ref paint);
                return 0;
            case WM_ERASEBKGND:
                return 1;
            case WM_TIMER:
                var bar = BarRect(hwnd);
                InvalidateRect(hwnd, ref bar, false);
                return 0;
            case WM_DPICHANGED:
                _dpi = (int)(wParam & 0xFFFF);
                CreateFonts();
                CenterOn(hwnd, MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST));
                InvalidateRect(hwnd, 0, false);
                return 0;
            case WM_NCHITTEST:
                //draggable, so it can be moved out of the way during a long migration
                return HTCAPTION;
            case WM_CLOSE:
                AnimateWindow(hwnd, 150, AW_BLEND | AW_HIDE);
                DestroyWindow(hwnd);
                return 0;
            case WM_DESTROY:
                PostQuitMessage(0);
                return 0;
        }
        return DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    private static RECT BarRect(nint hwnd)
    {
        GetClientRect(hwnd, out var client);
        return new RECT { Left = Px(70), Right = client.Right - Px(70), Top = Px(206), Bottom = Px(206) + Math.Max(2, Px(3)) };
    }

    private static void Paint(nint hwnd, nint windowDc)
    {
        GetClientRect(hwnd, out var client);
        //draw off screen, then copy, so the moving bar doesn't flicker
        var hdc = CreateCompatibleDC(windowDc);
        var bitmap = CreateCompatibleBitmap(windowDc, client.Right, client.Bottom);
        var oldBitmap = SelectObject(hdc, bitmap);

        Fill(hdc, client, BackgroundColor);
        if (_logo != 0 && GdipCreateFromHDC(hdc, out var graphics) == 0)
        {
            GdipSetInterpolationMode(graphics, InterpolationModeHighQualityBicubic);
            GdipDrawImageRectI(graphics, _logo, (client.Right - Px(LogoDip)) / 2, Px(28), Px(LogoDip), Px(LogoDip));
            GdipDeleteGraphics(graphics);
        }

        SetBkMode(hdc, TRANSPARENT);
        DrawLine(hdc, "FieldWorks Lite", _titleFont, White, client, Px(134), Px(30));
        DrawLine(hdc, _status, _statusFont, StatusColor, client, Px(170), Px(22));

        var bar = BarRect(hwnd);
        Fill(hdc, bar, TrackColor);
        //a segment a third of the track wide, sliding across every 1.4 s
        int trackWidth = bar.Right - bar.Left, segmentWidth = trackWidth / 3;
        var offset = (int)(Environment.TickCount64 % 1400 * (trackWidth + segmentWidth) / 1400) - segmentWidth;
        var segment = bar with { Left = Math.Max(bar.Left, bar.Left + offset), Right = Math.Min(bar.Right, bar.Left + offset + segmentWidth) };
        if (segment.Right > segment.Left) Fill(hdc, segment, White);

        BitBlt(windowDc, 0, 0, client.Right, client.Bottom, hdc, 0, 0, SRCCOPY);
        SelectObject(hdc, oldBitmap);
        DeleteObject(bitmap);
        DeleteDC(hdc);
    }

    private static void Fill(nint hdc, RECT rect, int color)
    {
        var brush = CreateSolidBrush(color);
        FillRect(hdc, ref rect, brush);
        DeleteObject(brush);
    }

    private static void DrawLine(nint hdc, string text, nint font, int color, RECT client, int top, int height)
    {
        if (text.Length == 0) return;
        var rect = new RECT { Left = Px(16), Right = client.Right - Px(16), Top = top, Bottom = top + height };
        var oldFont = SelectObject(hdc, font);
        SetTextColor(hdc, color);
        DrawTextW(hdc, text, text.Length, ref rect, DT_CENTER | DT_SINGLELINE | DT_VCENTER | DT_END_ELLIPSIS | DT_NOPREFIX);
        SelectObject(hdc, oldFont);
    }
}
