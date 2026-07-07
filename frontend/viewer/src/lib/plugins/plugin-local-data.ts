import {PluginApiException} from './plugin-api-types';

/**
 * Local (non-synced) plugin bookkeeping. Plugins run in an opaque origin without storage of
 * their own, so the host persists their key-value data here, namespaced per project + plugin.
 * Kept on raw localStorage rather than IPreferencesService because keys are dynamic per plugin.
 */

const STORAGE_PREFIX = 'fwlite-plugin-data';
const CONSENT_PREFIX = 'fwlite-plugin-consent';

/** Per-plugin storage budget; plugins get an explicit storage-full error beyond this. */
const MAX_PLUGIN_STORAGE_BYTES = 256 * 1024;

export class PluginStorage {
  #key: string;

  constructor(projectCode: string, pluginId: string) {
    this.#key = `${STORAGE_PREFIX}:${projectCode}:${pluginId}`;
  }

  #read(): Record<string, unknown> {
    const json = localStorage.getItem(this.#key);
    if (!json) return {};
    return JSON.parse(json) as Record<string, unknown>;
  }

  get(key: string): unknown {
    return this.#read()[key] ?? null;
  }

  set(key: string, value: unknown): void {
    const data = this.#read();
    data[key] = value;
    const json = JSON.stringify(data);
    if (json.length > MAX_PLUGIN_STORAGE_BYTES) {
      throw new PluginApiException('storage-full', `Plugin storage limit of ${MAX_PLUGIN_STORAGE_BYTES / 1024}KB exceeded`);
    }
    localStorage.setItem(this.#key, json);
  }

  remove(key: string): void {
    const data = this.#read();
    delete data[key];
    localStorage.setItem(this.#key, JSON.stringify(data));
  }

  clear(): void {
    localStorage.removeItem(this.#key);
  }
}

export async function computePluginHash(html: string): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(html));
  return [...new Uint8Array(digest)].map(byte => byte.toString(16).padStart(2, '0')).join('');
}

/**
 * Tracks which plugin content the current user has approved to run. Keyed by content hash so
 * an edited/synced plugin (even with the same id) requires fresh consent.
 */
export class PluginConsentStore {
  #key: string;

  constructor(projectCode: string) {
    this.#key = `${CONSENT_PREFIX}:${projectCode}`;
  }

  #read(): Record<string, string> {
    const json = localStorage.getItem(this.#key);
    if (!json) return {};
    return JSON.parse(json) as Record<string, string>;
  }

  isApproved(pluginId: string, contentHash: string): boolean {
    return this.#read()[pluginId] === contentHash;
  }

  approve(pluginId: string, contentHash: string): void {
    const data = this.#read();
    data[pluginId] = contentHash;
    localStorage.setItem(this.#key, JSON.stringify(data));
  }
}
