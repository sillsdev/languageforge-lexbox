import {
  KNOWN_PLUGIN_CONTEXTS,
  KNOWN_PLUGIN_HOST_FEATURES,
  KNOWN_PLUGIN_PERMISSIONS,
  type PluginContext,
  type PluginHostFeature,
  type PluginPermission,
} from './plugin-api-types';

/**
 * What a plugin declares about itself in its HTML `<meta name="fwlite-plugin-*">` tags.
 * The manifest lives in the HTML (single file stays the whole truth); it's extracted into the
 * Plugin entity when saved — so lists and menus never need the file — and re-parsed from the
 * actual HTML whenever it matters for safety (consent screen, sandbox CSP).
 */
export interface PluginManifest {
  permissions: PluginPermission[];
  contexts: PluginContext[];
  requires: PluginHostFeature[];
}

export function parsePluginManifest(html: string): PluginManifest {
  const doc = new DOMParser().parseFromString(html, 'text/html');
  return {
    permissions: parseMetaTokens(doc, 'fwlite-plugin-permissions', KNOWN_PLUGIN_PERMISSIONS),
    contexts: parseMetaTokens(doc, 'fwlite-plugin-contexts', KNOWN_PLUGIN_CONTEXTS),
    requires: parseMetaTokens(doc, 'fwlite-plugin-requires', KNOWN_PLUGIN_HOST_FEATURES),
  };
}

/**
 * Reads a manifest off an already-saved plugin entity, tolerating unknown tokens (which a newer
 * client may have written) by dropping them.
 */
export function manifestFromStored(stored: {permissions: string[]; contexts: string[]; requires: string[]}): PluginManifest {
  return {
    permissions: KNOWN_PLUGIN_PERMISSIONS.filter(token => stored.permissions.includes(token)),
    contexts: KNOWN_PLUGIN_CONTEXTS.filter(token => stored.contexts.includes(token)),
    requires: KNOWN_PLUGIN_HOST_FEATURES.filter(token => stored.requires.includes(token)),
  };
}

function parseMetaTokens<T extends string>(doc: Document, metaName: string, known: readonly T[]): T[] {
  const content = doc.querySelector(`meta[name="${metaName}"]`)?.getAttribute('content') ?? '';
  const requested = content.split(/[\s,]+/).filter(Boolean);
  return known.filter(token => requested.includes(token));
}
