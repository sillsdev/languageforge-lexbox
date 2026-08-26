/**
 * Channel to flags. The production channel is empty and must not appear here.
 * The `dev` channel is special-cased (all flags + DevContent) and must not appear here.
 * When a feature ships, remove the flag instead of adding it to production.
 *
 * Usage: `if (hasFlag('flag-name')) { ... }` or `<FlagContent flag="flag-name">`.
 */
export const CHANNEL_FLAGS = {
  beta: ['comments'],
} as const satisfies Record<string, readonly string[]>;

export type FeatureFlag = (typeof CHANNEL_FLAGS)[keyof typeof CHANNEL_FLAGS][number];

export const DEV_CHANNEL = 'dev';
export const STORAGE_KEY = 'fwlite-release-channel';
export const LEGACY_DEV_MODE_KEY = 'devMode';

export function normalizeChannel(value: string): string {
  return value.trim().toLowerCase();
}

export function isDevChannel(channel: string): boolean {
  return normalizeChannel(channel) === DEV_CHANNEL;
}

export function flagsForChannel(
  channel: string,
  channelFlags: Readonly<Record<string, readonly string[]>> = CHANNEL_FLAGS,
): readonly string[] {
  const normalized = normalizeChannel(channel);
  if (!normalized) return [];
  return channelFlags[normalized] ?? [];
}

export function channelHasFlag(
  flag: string,
  channel: string,
  channelFlags: Readonly<Record<string, readonly string[]>> = CHANNEL_FLAGS,
): boolean {
  if (isDevChannel(channel)) return true;
  if (channel === '') return false;//production channel has no flags
  return flagsForChannel(channel, channelFlags).includes(flag);
}

export function readStoredChannel(
  storage: Pick<Storage, 'getItem' | 'setItem' | 'removeItem'> | undefined = globalThis.localStorage,
): string {
  try {
    if (!storage) return '';
    const stored = normalizeChannel(storage.getItem(STORAGE_KEY) ?? '');
    if (stored) return stored;
    const legacyDev = storage.getItem(LEGACY_DEV_MODE_KEY) === 'true';
    if (legacyDev) {
      storage.removeItem(LEGACY_DEV_MODE_KEY);
      storage.setItem(STORAGE_KEY, DEV_CHANNEL);
      return DEV_CHANNEL;
    }
    return '';
  } catch {
    return '';
  }
}

export function persistChannel(
  channel: string,
  storage: Pick<Storage, 'setItem' | 'removeItem'> | undefined = globalThis.localStorage,
): void {
  try {
    if (!storage) return;
    const normalized = normalizeChannel(channel);
    if (normalized) {
      storage.setItem(STORAGE_KEY, normalized);
    }
    else {
      storage.removeItem(STORAGE_KEY);
    }
    storage.removeItem(LEGACY_DEV_MODE_KEY);
  } catch {
    // ignore quota / private mode
  }
}
