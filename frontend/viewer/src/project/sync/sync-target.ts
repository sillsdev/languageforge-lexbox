/** The syncs the sync dialog can run: FW Lite with Lexbox, Lexbox's FW Lite and FieldWorks Classic copies, FW Lite with
 * a sync folder, or with nearby devices over Wi-Fi */
export type SyncTarget = 'lite' | 'classic' | 'folder' | 'nearby';

/** The entries in the sync dialog's list of sync methods */
export type SyncMethod = SyncTarget;
