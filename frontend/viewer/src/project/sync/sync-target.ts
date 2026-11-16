/** The syncs the sync dialog can run: FW Lite with Lexbox, Lexbox's FW Lite and FieldWorks Classic copies, or FW Lite with a sync folder */
export type SyncTarget = 'lite' | 'classic' | 'folder';

/** The entries in the sync dialog's list of sync methods; nearby devices can't sync yet */
export type SyncMethod = SyncTarget | 'nearby';
