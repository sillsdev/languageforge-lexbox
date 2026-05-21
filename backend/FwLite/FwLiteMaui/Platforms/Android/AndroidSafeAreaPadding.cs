#if ANDROID
using Android.App;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using AView = Android.Views.View;
using AInsets = AndroidX.Core.Graphics.Insets;

namespace FwLiteMaui.Platforms.Android;

// Alternative to "push CSS custom properties into the WebView": instead, we shrink the
// activity's content area to sit inside the system-bar safe area. The WebView (which
// fills the content view) ends up the same shape it had pre-Android-15, and the slivers
// behind the status / nav bars reveal the activity window background, which we tint
// with a theme-aware color (values vs values-night) so it doesn't look like raw
// white/black. Zero frontend changes required.
//
// Trade-off: loses the "content scrolls behind the gesture nav" effect. That's fine for
// this app -- entries already terminate in chrome, not in scrolling content -- and
// matches how the app looked before SDK 35 enforced edge-to-edge.
internal static class AndroidSafeAreaPadding
{
    public static void Install(Activity activity)
    {
        // Window background shows through the status/nav bar gutters. Use a color resource
        // (statusBarBackground in values vs values-night) so it follows light/dark.
        activity.Window?.SetBackgroundDrawableResource(Resource.Color.statusBarBackground);

        ApplyStatusBarIconAppearance(activity);

        // android.R.id.content is the FrameLayout MAUI puts its layout under. Padding it
        // shrinks every descendant (including the BlazorWebView) into the safe area.
        var contentRoot = activity.FindViewById(global::Android.Resource.Id.Content);
        if (contentRoot is null) return;
        ViewCompat.SetOnApplyWindowInsetsListener(contentRoot, new InsetsListener());
        ViewCompat.RequestApplyInsets(contentRoot);
    }

    // Called both on Install and on UiMode changes (see MainActivity.OnConfigurationChanged).
    // ConfigChanges.UiMode keeps the activity alive across dark/light toggles, so we have
    // to re-pick the icon appearance manually -- Android won't redo it for us.
    public static void ApplyStatusBarIconAppearance(Activity activity)
    {
        if (activity.Window is not { } w) return;
        var nightMode = (activity.Resources?.Configuration?.UiMode & UiMode.NightMask) == UiMode.NightYes;
        var controller = WindowCompat.GetInsetsController(w, w.DecorView);
        if (controller is null) return;
        // true => dark icons (good on a light band); false => light icons (good on a dark band).
        controller.AppearanceLightStatusBars = !nightMode;
        controller.AppearanceLightNavigationBars = !nightMode;
    }

    private sealed class InsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat? OnApplyWindowInsets(AView? v, WindowInsetsCompat? insets)
        {
            if (v is null || insets is null) return insets;
            // SystemBars covers status + nav; DisplayCutout covers notches/punch-holes.
            // We deliberately do NOT include Ime here -- the activity has WindowSoftInputModeAdjust="Resize"
            // (see App.xaml), so Android already resizes the content view when the keyboard appears.
            // Adding Ime to the padding mask on top of that would double-pad.
            var mask = WindowInsetsCompat.Type.SystemBars()
                | WindowInsetsCompat.Type.DisplayCutout();
            var i = insets.GetInsets(mask);
            if (i is not null) v.SetPadding(i.Left, i.Top, i.Right, i.Bottom);
            return insets;
        }
    }
}
#endif
