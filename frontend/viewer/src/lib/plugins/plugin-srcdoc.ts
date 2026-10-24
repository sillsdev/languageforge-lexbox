import pluginSdkSource from './plugin-sdk.js?raw';
import type {PluginPermission} from './plugin-api-types';

/**
 * Sandbox flags for the plugin iframe. Deliberately NO allow-same-origin: plugins run in an
 * opaque origin with no access to the app's DOM, cookies, storage or services — the injected
 * postMessage SDK is their only bridge back. No allow-popups / allow-top-navigation either, so a
 * plugin can't open windows or navigate the app. It CAN still navigate its own frame (no sandbox
 * flag or CSP directive prevents scripted self-navigation), which is why PluginRunView tears the
 * plugin down if its frame ever loads a second document.
 */
export const PLUGIN_IFRAME_SANDBOX = 'allow-scripts allow-forms allow-modals allow-downloads';

/**
 * Permissions-Policy delegated to the plugin iframe. Microphone/camera let plugins record
 * pronunciation audio or capture pictures; the browser still prompts the user at capture time, so
 * no plugin-declared permission is needed.
 */
export const PLUGIN_IFRAME_ALLOW = 'microphone; camera';

/**
 * Blocks fetch-style network for plugins without the `internet` permission (connect/img/media/font
 * all fall back to default-src 'none'). Inline script/style stay allowed (that's the plugin
 * itself); data:/blob: URIs keep embedded images and fonts working. This does NOT block frame
 * self-navigation — see PLUGIN_IFRAME_SANDBOX — so it's a strong barrier, not an absolute one.
 */
const OFFLINE_CSP =
  "default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; img-src data: blob:; media-src data: blob:; font-src data: blob:";

/**
 * Prepares plugin HTML for the sandboxed iframe: injects the SDK (and, for offline plugins,
 * a network-blocking CSP) as the first children of the document's head.
 *
 * Injection goes through a real parse → mutate → serialize cycle rather than string splicing:
 * a regex/splice approach can be pointed at a fake `<head>` inside an attribute value or HTML
 * comment, leaving the CSP and SDK inert while the plugin runs unrestricted. Parsed nodes can't
 * be misplaced that way, and a CSP meta as the first head child guards everything after it.
 */
export function buildPluginSrcdoc(html: string, permissions: PluginPermission[]): string {
  const doc = new DOMParser().parseFromString(html, 'text/html');
  const sdkScript = doc.createElement('script');
  sdkScript.textContent = pluginSdkSource;
  doc.head.prepend(sdkScript);
  if (!permissions.includes('internet')) {
    const csp = doc.createElement('meta');
    csp.setAttribute('http-equiv', 'Content-Security-Policy');
    csp.setAttribute('content', OFFLINE_CSP);
    doc.head.prepend(csp);
  }
  const doctype = doc.doctype ? `<!DOCTYPE ${doc.doctype.name}>` : '';
  return doctype + doc.documentElement.outerHTML;
}
