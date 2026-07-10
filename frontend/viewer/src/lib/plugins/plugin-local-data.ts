import {PluginApiException} from './plugin-api-types';

/**
 * Local (non-synced) plugin bookkeeping. Plugins run in an opaque origin without storage of
 * their own, so the host persists their key-value data here, namespaced per project + plugin.
 * Kept on raw localStorage rather than IPreferencesService because keys are dynamic per plugin.
 */

const STORAGE_PREFIX = 'fwlite-plugin-data';
const CONSENT_PREFIX = 'fwlite-plugin-consent';
const WRITE_TRUST_PREFIX = 'fwlite-plugin-write-trust';

/** Per-plugin storage budget; plugins get an explicit storage-full error beyond this. */
const MAX_PLUGIN_STORAGE_BYTES = 256 * 1024;

export class PluginStorage {
  #key: string;

  constructor(projectCode: string, pluginId: string) {
    this.#key = `${STORAGE_PREFIX}:${projectCode}:${pluginId}`;
  }

  #read(): Record<string, unknown> {
    const json = localStorage.getItem(this.#key);
    // Null prototype so plugin-chosen keys like '__proto__' stay plain data.
    if (!json) return Object.create(null) as Record<string, unknown>;
    return Object.assign(Object.create(null), JSON.parse(json)) as Record<string, unknown>;
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

export async function sha256Hex(text: string): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(text));
  return [...new Uint8Array(digest)].map(byte => byte.toString(16).padStart(2, '0')).join('');
}

/**
 * Hash of everything trust decisions are about: the code AND the displayed identity. Including
 * the name/description means a synced rename can't silently keep riding an existing approval —
 * the name is what users recognize in the consent and write dialogs.
 */
export function computePluginHash(plugin: {name: string; description?: string; html: string}): Promise<string> {
  return sha256Hex(JSON.stringify([plugin.name, plugin.description ?? '', plugin.html]));
}

/**
 * A device-local map of pluginId → approved content hash, backing one yes/no decision per plugin.
 * Trust is deliberately personal and unsynced (a teammate's click must not authorize code on this
 * device) and hash-pinned (changed content asks again). Local edits through the editor re-pin the
 * hash silently — the user editing their own plugin IS the consent — so only changes that arrive
 * from elsewhere downgrade back to asking.
 */
class HashPinnedGrantStore {
  #key: string;

  constructor(prefix: string, projectCode: string) {
    this.#key = `${prefix}:${projectCode}`;
  }

  #read(): Record<string, string> {
    const json = localStorage.getItem(this.#key);
    if (!json) return {};
    return JSON.parse(json) as Record<string, string>;
  }

  isGranted(pluginId: string, contentHash: string): boolean {
    return this.#read()[pluginId] === contentHash;
  }

  /** True if this plugin holds a grant for ANY content version (used for re-pinning decisions). */
  grantedHash(pluginId: string): string | undefined {
    return this.#read()[pluginId];
  }

  grant(pluginId: string, contentHash: string): void {
    const data = this.#read();
    data[pluginId] = contentHash;
    localStorage.setItem(this.#key, JSON.stringify(data));
  }

  revoke(pluginId: string): void {
    const data = this.#read();
    delete data[pluginId];
    localStorage.setItem(this.#key, JSON.stringify(data));
  }
}

/** Tracks which plugin content the current user has approved to RUN on this device. */
export class PluginConsentStore extends HashPinnedGrantStore {
  constructor(projectCode: string) {
    super(CONSENT_PREFIX, projectCode);
  }
}

/**
 * Tracks plugins the user chose "Always allow" for: their writes apply without a per-operation
 * dialog (an ambient notification still surfaces each one). Revocable from the plugin card.
 */
export class PluginWriteTrustStore extends HashPinnedGrantStore {
  constructor(projectCode: string) {
    super(WRITE_TRUST_PREFIX, projectCode);
  }
}
