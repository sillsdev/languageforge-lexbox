
export const enum ViewerSearchParam {
  EntryId = 'entryId',
  IndexCharacter = 'indexCharacter',
  Search = 'search',
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
  if (replace) window.history.replaceState({}, '', `${window.location.pathname}${paramString}`);
  else window.history.pushState({}, '', `${window.location.pathname}${paramString}`);
}
