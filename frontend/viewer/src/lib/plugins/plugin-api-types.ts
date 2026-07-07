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
}

/** A write a plugin has requested; the user must approve it before it is applied. */
export type PluginWriteOperation =
  | {kind: 'createEntry'; entry: IEntry; summary: string[]}
  | {kind: 'updateEntry'; before: IEntry; after: IEntry; summary: string[]};
