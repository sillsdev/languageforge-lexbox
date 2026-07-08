import pluginSdkSource from './plugin-sdk.js?raw';
import {KNOWN_PLUGIN_CONTEXTS, KNOWN_PLUGIN_PERMISSIONS, type PluginContext, type PluginPermission} from './plugin-api-types';

/**
 * Sandbox flags for the plugin iframe. Deliberately NO allow-same-origin: plugins run in an
 * opaque origin with no access to the app's DOM, cookies, storage or services — the injected
 * postMessage SDK is their only bridge back.
 */
export const PLUGIN_IFRAME_SANDBOX = 'allow-scripts allow-forms allow-modals allow-downloads';

/**
 * Permissions-Policy delegated to the plugin iframe. Microphone/camera let plugins record
 * pronunciation audio or capture pictures; the browser still prompts the user at capture time, so
 * no plugin-declared permission is needed. (An offline plugin's CSP still blocks sending anywhere.)
 */
export const PLUGIN_IFRAME_ALLOW = 'microphone; camera';

/**
 * Blocks all network for plugins without the `internet` permission. Inline script/style stay
 * allowed (that's the plugin itself); data:/blob: URIs keep embedded images and fonts working.
 */
const OFFLINE_CSP =
  "default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; img-src data: blob:; media-src data: blob:; font-src data: blob:";

/** Permissions a plugin declares via `<meta name="fwlite-plugin-permissions" content="internet">`. */
export function parsePluginPermissions(html: string): PluginPermission[] {
  return parseMetaTokens(html, 'fwlite-plugin-permissions', KNOWN_PLUGIN_PERMISSIONS);
}

/** Contexts a plugin declares via `<meta name="fwlite-plugin-contexts" content="entry">`. */
export function parsePluginContexts(html: string): PluginContext[] {
  return parseMetaTokens(html, 'fwlite-plugin-contexts', KNOWN_PLUGIN_CONTEXTS);
}

function parseMetaTokens<T extends string>(html: string, metaName: string, known: readonly T[]): T[] {
  const doc = new DOMParser().parseFromString(html, 'text/html');
  const content = doc.querySelector(`meta[name="${metaName}"]`)?.getAttribute('content') ?? '';
  const requested = content.split(/[\s,]+/).filter(Boolean);
  return known.filter(token => requested.includes(token));
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
