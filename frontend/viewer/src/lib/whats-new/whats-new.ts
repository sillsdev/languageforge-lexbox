/**
 * What WhatsNewDialog shows, newest first. Add an entry (and its text in the dialog) when something big
 * ships, dated the day it is added; delete entries once they are old news.
 */
export const WHATS_NEW_ENTRIES = [
  {id: 'comments', since: '2026-10-25'},
  {id: 'commenter-role', since: '2026-10-25'},
  {id: 'flex-collation', since: '2026-10-25'},
  {id: 'pictures', since: '2026-10-25'},
  {id: 'error-reports', since: '2026-10-25'},
  {id: 'project-upgrade', since: '2026-10-25'},
] as const;

export type WhatsNewEntryId = (typeof WHATS_NEW_ENTRIES)[number]['id'];

/** Entries older than this stop showing, so the dialog never fills up with old news. */
export const WHATS_NEW_MAX_AGE_DAYS = 180;

/**
 * What the toast was last shown for: the newest entry date plus how many entries carry it, so adding an entry
 * (even on the same day) announces once and deleting one never does.
 */
export const WHATS_NEW_VERSION = whatsNewVersion(WHATS_NEW_ENTRIES);

function whatsNewVersion(entries: readonly {since: string}[]): string {
  const newest = entries.map(e => e.since).sort().at(-1) ?? '';
  return `${newest}/${entries.filter(e => e.since === newest).length}`;
}

function isNewerThan(version: string, seenVersion: string): boolean {
  const [date, count] = version.split('/');
  const [seenDate, seenCount] = seenVersion.split('/');
  return date > seenDate || (date === seenDate && Number(count) > Number(seenCount ?? 0));
}

/** Not on a fresh install (nothing is new to someone who has nothing yet), and never with nothing to show. */
export function shouldAnnounceWhatsNew(seenVersion: string | undefined, hasLocalProjects: boolean, today = new Date()): boolean {
  if (currentWhatsNewEntries(today).length === 0) return false;
  if (seenVersion && !isNewerThan(WHATS_NEW_VERSION, seenVersion)) return false;
  return !!seenVersion || hasLocalProjects;
}

export function currentWhatsNewEntries(today = new Date()): WhatsNewEntryId[] {
  const cutoff = new Date(today);
  cutoff.setDate(cutoff.getDate() - WHATS_NEW_MAX_AGE_DAYS);
  const cutoffDate = cutoff.toISOString().slice(0, 10);
  return WHATS_NEW_ENTRIES.filter(e => e.since >= cutoffDate).map(e => e.id);
}
