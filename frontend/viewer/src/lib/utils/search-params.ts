
export enum BrowseParam {
  EntryId = 'entryId',
  EntryOpen = 'entryOpen',
}

export enum ActivityParam {
  DetailOpen = 'detailOpen',
}

/**
 * Returns the query params needed to navigate to a specific entry in the browse view.
 * Always use this instead of constructing entryId params manually — entryOpen=true is
 * required for the entry to be visible on mobile.
 */
export function entryBrowseParams(entryId: string): string {
  return `${BrowseParam.EntryId}=${encodeURIComponent(entryId)}&${BrowseParam.EntryOpen}=true`;
}
