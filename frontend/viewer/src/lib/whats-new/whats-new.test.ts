import {describe, expect, it} from 'vitest';
import {WHATS_NEW_ENTRIES, WHATS_NEW_VERSION, currentWhatsNewEntries, shouldAnnounceWhatsNew} from './whats-new';

describe('WHATS_NEW_ENTRIES', () => {
  it('dates every entry as YYYY-MM-DD, which the string comparisons rely on', () => {
    for (const entry of WHATS_NEW_ENTRIES) expect(entry.since).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });
});

describe('shouldAnnounceWhatsNew', () => {
  it('announces when a newer entry was added since the last announcement', () => {
    expect(shouldAnnounceWhatsNew('2000-01-01', false)).toBe(true);
  });

  it('announces when nothing was stored yet but projects exist (upgrade from before this feature)', () => {
    expect(shouldAnnounceWhatsNew(undefined, true)).toBe(true);
  });

  it('stays quiet on a fresh install', () => {
    expect(shouldAnnounceWhatsNew(undefined, false)).toBe(false);
  });

  it('stays quiet once the newest entry was announced, also after older entries are deleted', () => {
    expect(shouldAnnounceWhatsNew(WHATS_NEW_VERSION, true)).toBe(false);
    expect(shouldAnnounceWhatsNew('2999-01-01/1', true)).toBe(false);
  });

  it('announces an entry added on the same day as the newest one', () => {
    const [date, count] = WHATS_NEW_VERSION.split('/');
    expect(shouldAnnounceWhatsNew(`${date}/${Number(count) - 1}`, true)).toBe(true);
  });

  it('stays quiet when every entry has aged out, even for a returning user', () => {
    expect(shouldAnnounceWhatsNew(undefined, true, new Date('2999-01-01'))).toBe(false);
    expect(shouldAnnounceWhatsNew('2000-01-01', true, new Date('2999-01-01'))).toBe(false);
  });
});

describe('currentWhatsNewEntries', () => {
  it('shows every entry while it is recent', () => {
    expect(currentWhatsNewEntries(new Date(WHATS_NEW_VERSION.split('/')[0]))).toEqual(WHATS_NEW_ENTRIES.map(e => e.id));
  });

  it('drops entries once they are old news', () => {
    expect(currentWhatsNewEntries(new Date('2999-01-01'))).toEqual([]);
  });
});
