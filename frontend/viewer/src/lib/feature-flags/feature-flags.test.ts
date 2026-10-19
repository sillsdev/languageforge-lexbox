import {afterEach, describe, expect, it} from 'vitest';

import {
  channelHasFlag,
  DEV_CHANNEL,
  flagsForChannel,
  isDevChannel,
  LEGACY_DEV_MODE_KEY,
  normalizeChannel,
  persistChannel,
  readStoredChannel,
  STORAGE_KEY,
} from './feature-flags';

const TEST_FLAGS = {
  beta: ['flag-one', 'flag-two'],
  alpha: ['flag-one', 'flag-two', 'flag-three'],
} as const;

afterEach(() => {
  localStorage.removeItem(STORAGE_KEY);
  localStorage.removeItem(LEGACY_DEV_MODE_KEY);
});

describe('normalizeChannel', () => {
  it('trims and lowercases', () => {
    expect(normalizeChannel('  Beta ')).toBe('beta');
  });

  it('treats blank as production', () => {
    expect(normalizeChannel('   ')).toBe('');
  });
});

describe('flagsForChannel', () => {
  it('returns no flags for production', () => {
    expect(flagsForChannel('', TEST_FLAGS)).toEqual([]);
    expect(flagsForChannel('   ', TEST_FLAGS)).toEqual([]);
  });

  it('returns flags for a known channel', () => {
    expect(flagsForChannel('BETA', TEST_FLAGS)).toEqual(['flag-one', 'flag-two']);
  });

  it('returns no flags for an unknown channel', () => {
    expect(flagsForChannel('nightly', TEST_FLAGS)).toEqual([]);
  });
});

describe('channelHasFlag', () => {
  it('is true only when the channel lists the flag', () => {
    expect(channelHasFlag('flag-three', 'alpha', TEST_FLAGS)).toBe(true);
    expect(channelHasFlag('flag-three', 'beta', TEST_FLAGS)).toBe(false);
    expect(channelHasFlag('flag-three', '', TEST_FLAGS)).toBe(false);
  });

  it('is true for every flag on the dev channel', () => {
    expect(channelHasFlag('flag-three', DEV_CHANNEL, TEST_FLAGS)).toBe(true);
    expect(channelHasFlag('undeclared', 'DEV')).toBe(true);
  });
});

describe('isDevChannel', () => {
  it('matches the dev channel case-insensitively', () => {
    expect(isDevChannel('dev')).toBe(true);
    expect(isDevChannel(' DEV ')).toBe(true);
    expect(isDevChannel('beta')).toBe(false);
    expect(isDevChannel('')).toBe(false);
  });
});

describe('production channel', () => {
  it('never enables flags', () => {
    expect(flagsForChannel('')).toEqual([]);
    expect(channelHasFlag('anything', '')).toBe(false);
  });
});

describe('channel persistence', () => {
  it('stores a channel and clears production', () => {
    persistChannel('beta');
    expect(localStorage.getItem(STORAGE_KEY)).toBe('beta');
    expect(readStoredChannel()).toBe('beta');

    persistChannel('');
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
    expect(readStoredChannel()).toBe('');
  });

  it('migrates legacy devMode to the dev channel when empty', () => {
    localStorage.setItem(LEGACY_DEV_MODE_KEY, 'true');
    expect(readStoredChannel()).toBe(DEV_CHANNEL);
    expect(localStorage.getItem(STORAGE_KEY)).toBe(DEV_CHANNEL);
    expect(localStorage.getItem(LEGACY_DEV_MODE_KEY)).toBeNull();
  });

  it('keeps an existing channel when migrating legacy devMode', () => {
    localStorage.setItem(STORAGE_KEY, 'beta');
    localStorage.setItem(LEGACY_DEV_MODE_KEY, 'true');
    expect(readStoredChannel()).toBe('beta');
    expect(localStorage.getItem(LEGACY_DEV_MODE_KEY)).toBeNull();
  });
});
