
export const enum ViewerSearchParam {
  EntryId = 'entryId',
  IndexCharacter = 'indexCharacter',
  Search = 'search',
}

const urlSearchParams = new URLSearchParams(window.location.search);

export function getSearchParam(param: ViewerSearchParam): string | undefined {
  return urlSearchParams.get(param) ?? undefined;
}

export function updateSearchParam(param: ViewerSearchParam, value: string | undefined, replace: boolean) {
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
