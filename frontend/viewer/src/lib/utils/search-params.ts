
export const enum ViewerSearchParam {
  EntryId = 'entryId',
  EntryOpen = 'entryOpen',
  IndexCharacter = 'indexCharacter',
  Search = 'search',
}

/**
 * Returns the query params needed to navigate to a specific entry in the browse view.
 * Always use this instead of constructing entryId params manually — entryOpen=true is
 * required for the entry to be visible on mobile.
 */
export function entryBrowseParams(entryId: string): string {
  return `${ViewerSearchParam.EntryId}=${encodeURIComponent(entryId)}&${ViewerSearchParam.EntryOpen}=true`;
}
export function getSearchParams(): URLSearchParams {
  return new URLSearchParams(window.location.search);
}

export function getSearchParam(param: ViewerSearchParam): string | null {
  return getSearchParams().get(param);
}

export function updateSearchParam(param: ViewerSearchParam, value: string | undefined | null, replace: boolean) {
  const urlSearchParams = getSearchParams();
  const current = urlSearchParams.get(param) ?? undefined;
  if (current == value) return;
  if (value) urlSearchParams.set(param, value);
  else urlSearchParams.delete(param);
  let paramString = urlSearchParams.toString();
  if (paramString.length) paramString = `?${paramString}`;
  //this can fail in electron, work around it for now
  if ('electronAPI' in window || (window.top && 'electronAPI' in window.top)) return;
  try {
    if (replace) window.history.replaceState({}, '', `${window.location.pathname}${paramString}`);
    else window.history.pushState({}, '', `${window.location.pathname}${paramString}`);
  }
  catch (e) {
    console.error('Could not update search param', param, value, e);
  }
}
