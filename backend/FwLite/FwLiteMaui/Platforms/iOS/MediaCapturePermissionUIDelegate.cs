using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using WebKit;

namespace FwLiteMaui.Platforms.iOS;

/// <summary>
/// Without a requestMediaCapturePermission handler, WKWebView shows its own "Allow “FieldWorks Lite” to use your
/// microphone?" prompt on every app launch, on top of the one-time iOS permission. This grants microphone capture
/// to the app's own app:// pages; iOS still asks once and the user can revoke it in Settings.
/// Wraps BlazorWebView's UI delegate, which implements the JS alert/confirm/prompt panels, and forwards those to it.
/// </summary>
internal class MediaCapturePermissionUIDelegate(WKUIDelegate inner) : WKUIDelegate
{
    // BlazorWebView exports this selector itself (the bound override can't return null, dotnet/macios#15728),
    // so forward the raw arguments to it rather than going through the obsolete managed override.
    private const string TextInputPanelSelector =
        "webView:runJavaScriptTextInputPanelWithPrompt:defaultText:initiatedByFrame:completionHandler:";

    public override void RequestMediaCapturePermission(WKWebView webView, WKSecurityOrigin origin, WKFrameInfo frame,
        WKMediaCaptureType type, Action<WKPermissionDecision> decisionHandler)
    {
        var isAppOrigin = string.Equals(origin.Protocol, "app", StringComparison.OrdinalIgnoreCase);
        decisionHandler(isAppOrigin && type == WKMediaCaptureType.Microphone
            ? WKPermissionDecision.Grant
            : WKPermissionDecision.Prompt);
    }

    public override void RunJavaScriptAlertPanel(WKWebView webView, string message, WKFrameInfo frame,
        Action completionHandler) => inner.RunJavaScriptAlertPanel(webView, message, frame, completionHandler);

    public override void RunJavaScriptConfirmPanel(WKWebView webView, string message, WKFrameInfo frame,
        Action<bool> completionHandler) => inner.RunJavaScriptConfirmPanel(webView, message, frame, completionHandler);

    [Export(TextInputPanelSelector)]
    public void ForwardTextInputPanel(IntPtr webView, IntPtr prompt, IntPtr defaultText, IntPtr frame,
        IntPtr completionHandler)
    {
        ForwardTextInputPanel(inner.Handle, Selector.GetHandle(TextInputPanelSelector),
            webView, prompt, defaultText, frame, completionHandler);
    }

    // Only claim the prompt panel when the wrapped delegate can actually complete it.
    public override bool RespondsToSelector(Selector? sel) =>
        sel?.Name == TextInputPanelSelector ? inner.RespondsToSelector(sel) : base.RespondsToSelector(sel);

    [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void ForwardTextInputPanel(IntPtr receiver, IntPtr selector, IntPtr webView, IntPtr prompt,
        IntPtr defaultText, IntPtr frame, IntPtr completionHandler);
}
