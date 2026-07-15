import type {IEntry} from '$lib/dotnet-types';

/**
 * Plugin API v1 — the frozen contract between FW Lite and sandboxed plugins.
 * Breaking changes require a new version; additions are fine.
 */
export const PLUGIN_API_VERSION = 1;

/** Message source discriminators. Plugins send `fwlite-plugin`, the host sends `fwlite-plugin-host`. */
export const PLUGIN_MESSAGE_SOURCE = 'fwlite-plugin';
export const HOST_MESSAGE_SOURCE = 'fwlite-plugin-host';

/**
 * Permissions a plugin declares via `<meta name="fwlite-plugin-permissions" content="internet edit">`.
 * - `internet`: the network-blocking CSP is not injected (surfaced as a warning badge).
 * - `edit`: the plugin may request dictionary writes (each still user-approved). Without it, write
 *   methods reject immediately — a read-only plugin can never even show a write dialog.
 */
export type PluginPermission = 'internet' | 'edit';
export const KNOWN_PLUGIN_PERMISSIONS: PluginPermission[] = ['internet', 'edit'];

/**
 * Contexts a plugin declares it can be launched from, via
 * `<meta name="fwlite-plugin-contexts" content="entry">`. A plugin that doesn't declare `entry`
 * is not offered in an entry's context menu.
 */
export type PluginContext = 'entry';
export const KNOWN_PLUGIN_CONTEXTS: PluginContext[] = ['entry'];

/**
 * Optional host features. Not every host has them (comments and history live in the CRDT layer;
 * a future FieldWorks-backed host won't have either), so hosts advertise them in
 * {@link HostInitMessage.capabilities} and plugins declare the ones they can't work without via
 * `<meta name="fwlite-plugin-requires" content="comments history">` — letting a host warn before
 * launch instead of the plugin failing mid-run. Feature names are capabilities, not backend
 * implementation names, so they stay meaningful across hosts.
 */
export type PluginHostFeature = 'comments' | 'history';
export const KNOWN_PLUGIN_HOST_FEATURES: PluginHostFeature[] = ['comments', 'history'];

/**
 * How `openEntry` surfaces an entry. `view`/`edit` open a dialog over the still-mounted plugin;
 * `window` opens a separate window (unavailable on some platforms, e.g. Android/iOS, where the
 * host falls back to `view`); `navigate` leaves the plugin for the entry (loses plugin state).
 */
export type OpenEntryMode = 'view' | 'edit' | 'window' | 'navigate';
export const KNOWN_OPEN_ENTRY_MODES: OpenEntryMode[] = ['view', 'edit', 'window', 'navigate'];

export interface PluginRequestMessage {
  source: typeof PLUGIN_MESSAGE_SOURCE;
  v: number;
  kind: 'request';
  id: number;
  method: string;
  args: unknown[];
}

export interface PluginSdkReadyMessage {
  source: typeof PLUGIN_MESSAGE_SOURCE;
  v: number;
  kind: 'sdk-ready';
}

export type PluginMessage = PluginRequestMessage | PluginSdkReadyMessage;

export interface HostResponseMessage {
  source: typeof HOST_MESSAGE_SOURCE;
  v: number;
  kind: 'response';
  id: number;
  ok: boolean;
  result?: unknown;
  error?: PluginApiError;
}

/** What this host build/project supports, so plugins can feature-detect and degrade gracefully. */
export interface HostCapabilities {
  openEntryModes: OpenEntryMode[];
  comments: boolean;
  history: boolean;
}

export interface HostInitMessage {
  source: typeof HOST_MESSAGE_SOURCE;
  v: number;
  kind: 'init';
  apiVersion: number;
  project: {projectName: string; projectCode: string};
  theme: 'light' | 'dark';
  permissions: PluginPermission[];
  capabilities: HostCapabilities;
  context?: {entryId?: string};
}

export interface PluginApiError {
  code: PluginApiErrorCode;
  message: string;
}

export type PluginApiErrorCode =
  | 'unknown-method'
  | 'invalid-args'
  | 'permission-denied'
  | 'not-supported'
  | 'conflict'
  | 'storage-full'
  | 'internal';

export class PluginApiException extends Error {
  constructor(readonly code: PluginApiErrorCode, message: string) {
    super(message);
  }
}

export function isPluginMessage(data: unknown): data is PluginMessage {
  if (typeof data !== 'object' || data === null) return false;
  const message = data as Partial<PluginMessage>;
  return message.source === PLUGIN_MESSAGE_SOURCE && typeof message.kind === 'string';
}

/** Options plugins pass to getEntries. Filters are structured (no raw query language). */
export interface PluginEntryQuery {
  search?: string;
  limit?: number;
  offset?: number;
  filter?: PluginEntryFilter;
  sort?: {writingSystem?: string; ascending?: boolean};
}

export interface PluginEntryFilter {
  /** Semantic domain code, e.g. "2.1.1" */
  semanticDomainCode?: string;
  /** Part of speech id (GUID) */
  partOfSpeechId?: string;
  /** Writing system code; matches entries with no sense glossed in that writing system. */
  missingGlossWs?: string;
  /** Writing system code; matches entries where no sense has an example sentence with text in that writing system. */
  missingExampleWs?: string;
  /** Matches entries having a sense without a part of speech (or no senses). */
  missingPartOfSpeech?: boolean;
}

/** A write a plugin has requested; the user must approve it before it is applied. */
export type PluginWriteOperation =
  | {kind: 'createEntry'; entry: IEntry; summary: string[]}
  | {kind: 'updateEntry'; before: IEntry; after: IEntry; summary: string[]}
  | {kind: 'batch'; count: number; summary: string[]};
