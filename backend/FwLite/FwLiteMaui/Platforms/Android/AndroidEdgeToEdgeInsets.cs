#if ANDROID
using Android.Util;
using AndroidX.Core.View;
using AView = Android.Views.View;
using AWebView = Android.Webkit.WebView;
using AInsets = AndroidX.Core.Graphics.Insets;

namespace FwLiteMaui.Platforms.Android;

// Wires up edge-to-edge support for the Blazor WebView on Android 15+ (SDK 35).
// Reads the real system-bar insets (status bar at top, gesture-nav / 3-button nav at bottom)
// and writes them onto the WebView's root element as two tiers of CSS custom properties:
//
//   --android-safe-{top,right,bottom,left}  -- "chrome" inset, SystemBars + DisplayCutout
//      only. Used for general page chrome (status bar / nav bar / notch clearance) where
//      Material Design's standard safe area is appropriate. This is what .app pads with.
//
//   --android-wide-{top,right,bottom,left}  -- "wide" inset, chrome union with
//      MandatorySystemGestures + TappableElement. Used for FLOATING elements (FABs,
//      toasters) that need extra clearance from the gesture-nav tappable region.
//      Without this split, the gesture-area reservation bleeds into every page chrome
//      consumer and over-reserves visible space.
//
// Also writes --android-ime-bottom for the soft keyboard, kept separate from system bars
// so chrome surfaces (sidebars, drawers) can ignore it while scrollable content can shrink.
// CSS consumes them with sensible env() fallbacks,
// e.g. var(--android-safe-bottom, env(safe-area-inset-bottom)).
//
// We do this in JS rather than relying on CSS env(safe-area-inset-*) because that has been
// observed to report 0 inside the Android System WebView even with viewport-fit=cover.
//
// We must NOT replace the WebView's WebViewClient — Blazor's own client intercepts
// https://0.0.0.0/ requests and serves the embedded app. Anything that breaks that
// chain bricks the WebView. The insets-listener alone is enough; CSS vars set on
// document.documentElement.style survive Blazor's DOM swap.
internal static class AndroidEdgeToEdgeInsets
{
    public static void Install(AWebView webView)
    {
        // Belt-and-suspenders: ensure the WebView doesn't auto-pad behind our back on
        // OEM AppCompat themes that flip fitsSystemWindows=true via parent inheritance.
        // We're handling insets ourselves via the listener below.
        webView.SetFitsSystemWindows(false);
        ViewCompat.SetOnApplyWindowInsetsListener(webView, new InsetsListener(webView));
        ViewCompat.RequestApplyInsets(webView);
    }

    private sealed class InsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        private readonly AWebView _webView;
        private AInsets _lastChrome = AInsets.None!;
        private AInsets _lastWide = AInsets.None!;
        private int _lastImeBottom = -1;

        public InsetsListener(AWebView webView) => _webView = webView;

        public WindowInsetsCompat? OnApplyWindowInsets(AView? v, WindowInsetsCompat? insets)
        {
            if (insets is null) return insets;
            // Two-tier insets:
            //   chrome = SystemBars + DisplayCutout. Material Design's standard "safe area"
            //     for general page chrome. What .app pads with.
            //   wide   = chrome + MandatorySystemGestures + TappableElement. For floating
            //     elements (FABs, toasters) that must clear the gesture-nav tappable region
            //     which can be wider than the visual handle reported in SystemBars.
            //
            // IME is read separately (not unioned) so chrome surfaces can keep using the
            // system-only height while scrollable content shrinks for the keyboard.
            // WindowInsetsCompat.Type.Ime() backports keyboard dispatch on pre-API-30.
            var chromeMask = WindowInsetsCompat.Type.SystemBars()
                | WindowInsetsCompat.Type.DisplayCutout();
            var wideMask = chromeMask
                | WindowInsetsCompat.Type.MandatorySystemGestures()
                | WindowInsetsCompat.Type.TappableElement();
            var chrome = insets.GetInsets(chromeMask);
            var wide = insets.GetInsets(wideMask);
            var sysBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            var ime = insets.GetInsets(WindowInsetsCompat.Type.Ime());
            // Standard reduction: subtract nav-bar bottom so we don't double-count when
            // the IME sits behind an opaque nav bar.
            var imeBottom = Math.Max(0, (ime?.Bottom ?? 0) - (sysBars?.Bottom ?? 0));
            var chromeChanged = chrome is not null && !chrome.Equals(_lastChrome);
            var wideChanged = wide is not null && !wide.Equals(_lastWide);
            var imeChanged = imeBottom != _lastImeBottom;
            if (chromeChanged || wideChanged || imeChanged)
            {
                LogBreakdown(insets, imeBottom);
                if (chrome is not null) _lastChrome = chrome;
                if (wide is not null) _lastWide = wide;
                _lastImeBottom = imeBottom;
                Apply(_webView, _lastChrome, _lastWide, imeBottom);
            }
            // Don't consume - let MAUI's own listeners (if any) still see the insets.
            return insets;
        }

        private static void LogBreakdown(WindowInsetsCompat insets, int imeReducedBottom)
        {
            // Diagnostic: surface the individual inset categories so we can see on-device
            // (`adb logcat -s FwLiteInsets`) why the bottom value is what it is.
            var sb = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            var cut = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());
            var gest = insets.GetInsets(WindowInsetsCompat.Type.MandatorySystemGestures());
            var tap = insets.GetInsets(WindowInsetsCompat.Type.TappableElement());
            var ime = insets.GetInsets(WindowInsetsCompat.Type.Ime());
            Log.Debug("FwLiteInsets",
                $"SystemBars b={sb?.Bottom} t={sb?.Top}; Cutout b={cut?.Bottom} t={cut?.Top}; " +
                $"MandatoryGestures b={gest?.Bottom}; Tappable b={tap?.Bottom}; " +
                $"Ime b={ime?.Bottom} (reduced={imeReducedBottom})");
        }
    }

    private static void Apply(AWebView webView, AInsets chrome, AInsets wide, int imeBottomPx)
    {
        var density = webView.Resources?.DisplayMetrics?.Density ?? 1f;
        // The WebView's viewport is measured in CSS pixels; system insets come back in physical px.
        // Use ceiling so we never under-reserve by a sub-CSS-pixel - truncation could leak
        // up to ~1px of content under the bar at non-integer densities (e.g. 2.75x).
        var cTop = (int)Math.Ceiling(chrome.Top / density);
        var cRight = (int)Math.Ceiling(chrome.Right / density);
        var cBottom = (int)Math.Ceiling(chrome.Bottom / density);
        var cLeft = (int)Math.Ceiling(chrome.Left / density);
        var wTop = (int)Math.Ceiling(wide.Top / density);
        var wRight = (int)Math.Ceiling(wide.Right / density);
        var wBottom = (int)Math.Ceiling(wide.Bottom / density);
        var wLeft = (int)Math.Ceiling(wide.Left / density);
        var ime = (int)Math.Ceiling(imeBottomPx / density);
        Log.Debug("FwLiteInsets",
            $"Applied CSS px: chrome=({cTop},{cRight},{cBottom},{cLeft}) " +
            $"wide=({wTop},{wRight},{wBottom},{wLeft}) ime={ime} density={density}");
        var js = $$"""
            (function() {
              var s = document.documentElement.style;
              s.setProperty('--android-safe-top', '{{cTop}}px');
              s.setProperty('--android-safe-right', '{{cRight}}px');
              s.setProperty('--android-safe-bottom', '{{cBottom}}px');
              s.setProperty('--android-safe-left', '{{cLeft}}px');
              s.setProperty('--android-wide-top', '{{wTop}}px');
              s.setProperty('--android-wide-right', '{{wRight}}px');
              s.setProperty('--android-wide-bottom', '{{wBottom}}px');
              s.setProperty('--android-wide-left', '{{wLeft}}px');
              s.setProperty('--android-ime-bottom', '{{ime}}px');
            })();
            """;
        webView.Post(() => webView.EvaluateJavascript(js, null));
    }
}
#endif
