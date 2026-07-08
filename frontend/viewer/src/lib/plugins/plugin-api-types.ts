import type {IEntry} from '$lib/dotnet-types';

/**
 * Plugin API v1 — the frozen contract between FW Lite and sandboxed plugins.
 * Breaking changes require a new version; additions are fine.
 */
export const PLUGIN_API_VERSION = 1;

/** Message source discriminators. Plugins send `fwlite-plugin`, the host sends `fwlite-plugin-host`. */
export const PLUGIN_MESSAGE_SOURCE = 'fwlite-plugin';
export const HOST_MESSAGE_SOURCE = 'fwlite-plugin-host';

export type PluginPermission = 'internet';
export const KNOWN_PLUGIN_PERMISSIONS: PluginPermission[] = ['internet'];

/**
 * Contexts a plugin declares it can be launched from, via
 * `<meta name="fwlite-plugin-contexts" content="entry">`. A plugin that doesn't declare `entry`
 * is not offered in an entry's context menu.
 */
export type PluginContext = 'entry';
export const KNOWN_PLUGIN_CONTEXTS: PluginContext[] = ['entry'];

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

export interface HostInitMessage {
  source: typeof HOST_MESSAGE_SOURCE;
  v: number;
  kind: 'init';
  apiVersion: number;
  project: {projectName: string; projectCode: string};
  theme: 'light' | 'dark';
  permissions: PluginPermission[];
  /** What this host build supports, so plugins can feature-detect and degrade gracefully. */
  capabilities: {openEntryModes: OpenEntryMode[]};
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
