using AuthenticationServices;
using Foundation;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using UIKit;

namespace FwLiteMaui;

/// <summary>
/// Runs MSAL's interactive login in an <see cref="ASWebAuthenticationSession"/>, the same OS flow MSAL uses on iOS:
/// shares Safari's cookies (after the OS consent sheet), intercepts the custom-scheme redirect itself, and needs no
/// localhost listener. The MSAL package has no Mac Catalyst build, so without this it uses its desktop system-browser
/// flow, which makes Safari warn about an insecure form post to http://localhost.
/// </summary>
public class AuthenticationSessionWebUi : ICustomWebUi
{
    public async Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
        ASWebAuthenticationSession? session = null;
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var url = new NSUrl(authorizationUri.AbsoluteUri);
            void CompletionHandler(NSUrl? callbackUrl, NSError? error)
            {
                if (callbackUrl is not null)
                {
                    completion.TrySetResult(new Uri(callbackUrl.AbsoluteString!));
                }
                else if (error?.Code == (long)ASWebAuthenticationSessionErrorCode.CanceledLogin)
                {
                    completion.TrySetException(new MsalClientException(MsalError.AuthenticationCanceledError, "User canceled authentication."));
                }
                else
                {
                    completion.TrySetException(new MsalClientException(MsalError.AuthenticationFailed, error?.LocalizedDescription ?? "Authentication session failed"));
                }
            }

            // The (url, string scheme, handler) constructor is obsoleted on maccatalyst 17.4+; use the
            // ASWebAuthenticationSessionCallback overload there and fall back to the string overload on 15.0-17.3.
            if (OperatingSystem.IsMacCatalystVersionAtLeast(17, 4))
            {
                session = new ASWebAuthenticationSession(url,
                    ASWebAuthenticationSessionCallback.Create(redirectUri.Scheme), CompletionHandler);
            }
            else
            {
                session = new ASWebAuthenticationSession(url, redirectUri.Scheme, CompletionHandler);
            }

            // share the user's Safari session so an existing Lexbox login completes without re-entering credentials
            session.PrefersEphemeralWebBrowserSession = false;
            session.PresentationContextProvider = new PresentationContextProvider();

            if (!session.Start())
            {
                completion.TrySetException(new MsalClientException(MsalError.AuthenticationFailed, "Could not start the authentication session"));
            }
        });
        using var registration = cancellationToken.Register(() =>
        {
            MainThread.BeginInvokeOnMainThread(() => session?.Cancel());
            completion.TrySetCanceled(cancellationToken);
        });
        return await completion.Task;
    }

    private sealed class PresentationContextProvider : NSObject, IASWebAuthenticationPresentationContextProviding
    {
        public UIWindow GetPresentationAnchor(ASWebAuthenticationSession session)
        {
            return UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>()
                       .SelectMany(scene => scene.Windows).FirstOrDefault(w => w.IsKeyWindow)
                   ?? UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>().SelectMany(scene => scene.Windows).First();
        }
    }
}
