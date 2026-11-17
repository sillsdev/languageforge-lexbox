import pluginSdkSource from './plugin-sdk.js?raw';
import type {PluginPermission} from './plugin-api-types';

/**
 * Sandbox flags for the plugin iframe. Deliberately NO allow-same-origin: plugins run in an
 * opaque origin with no access to the app's DOM, cookies, storage or services — the injected
 * postMessage SDK is their only bridge back. No allow-popups / allow-top-navigation either, so a
 * plugin can't open windows or navigate the app. No flag stops it navigating its own frame:
 * installPluginFramePolicy does that.
 */
export const PLUGIN_IFRAME_SANDBOX = 'allow-scripts allow-forms allow-modals allow-downloads';

/**
 * Blocks fetch-style network for plugins without the `internet` permission (connect/img/media/font
 * all fall back to default-src 'none'; form-action is not covered by default-src, so it's listed).
 * Inline script/style stay allowed (that's the plugin itself); data:/blob: URIs keep embedded
 * images and fonts working. A document's own CSP can't stop it navigating its frame (see
 * installPluginFramePolicy), and WebRTC is outside CSP, so it's a strong barrier, not an absolute one.
 */
const OFFLINE_CSP =
  "default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; img-src data: blob:; media-src data: blob:; font-src data: blob:; form-action 'none'";

const PLUGIN_FRAME_POLICY = 'frame-src blob: data:';

/**
 * Stops a plugin navigating its own frame to another origin, where the URL could carry dictionary
 * data out. Only the EMBEDDING page's frame-src governs that, so the policy goes on the app page,
 * added from here so every host (FwLiteWeb, MAUI, the demo) gets it. Call before creating the iframe.
 *
 * It applies to every iframe on the app page and a CSP can't be taken back: an iframe that loads
 * anything but blob:/data: needs its source added here first. The plugin document inherits it too,
 * so plugins with the `internet` permission can't navigate away or embed other sites either.
 * blob: is the plugin document itself; data: is there because WebKit checks a `data:` download
 * link against frame-src. A document a plugin builds and navigates to keeps the plugin's own CSP.
 */
export function installPluginFramePolicy(): void {
  if (document.head.querySelector(`meta[content="${PLUGIN_FRAME_POLICY}"]`)) return;
  const csp = document.createElement('meta');
  csp.setAttribute('http-equiv', 'Content-Security-Policy');
  csp.setAttribute('content', PLUGIN_FRAME_POLICY);
  document.head.append(csp);
}

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

/**
 * The composed document as a revocable Blob URL for the iframe's `src`. The caller owns the URL
 * and revokes it when done. A Blob (not a `srcdoc` attribute or retained state) keeps multi-MB
 * plugin HTML GC-able once the caller's scope ends.
 */
export function createPluginDocUrl(html: string, permissions: PluginPermission[]): string {
  return URL.createObjectURL(new Blob([buildPluginSrcdoc(html, permissions)], {type: 'text/html'}));
}
