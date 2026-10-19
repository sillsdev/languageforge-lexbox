import {DotnetService} from '$lib/dotnet-types';
import {tryUseService} from '$lib/services/service-provider';

/**
 * Copy text to the clipboard.
 *
 * Prefers the browser Clipboard API, the known-good path on the web, Android and Windows WebViews.
 * That API is only exposed in a secure context, and the Apple WebViews (iOS / Mac Catalyst) serve
 * the app from an insecure `app://` scheme, so `navigator.clipboard` is `undefined` there. In that
 * case we bridge through the .NET platform-features service, which uses the native MAUI clipboard.
 *
 * Throws if neither path is available so callers can surface a failure rather than silently no-op.
 */
export async function copyText(text: string): Promise<void> {
  if (typeof navigator !== 'undefined' && navigator.clipboard) {
    await navigator.clipboard.writeText(text);
    return;
  }

  const platformFeatures = tryUseService(DotnetService.PlatformFeaturesService);
  if (platformFeatures) {
    await platformFeatures.copyToClipboard(text);
    return;
  }

  throw new Error('Clipboard is not available in this environment');
}
