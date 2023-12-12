import {browser} from '$app/environment';
import Cookies from 'js-cookie';
import type {LoadEvent, Cookies as KitCookies, RequestEvent} from '@sveltejs/kit';

export const STORAGE_VIEW_MODE_KEY = 'projectViewMode';
export function getViewMode(event: LoadEvent): ViewMode | undefined {
  if (browser) {
    return Cookies.get(STORAGE_VIEW_MODE_KEY) as ViewMode | undefined;
  } else {
    //stored in params by hooks.server.ts calling setViewMode
    return event.params[STORAGE_VIEW_MODE_KEY] as ViewMode | undefined;
  }
}

export function setViewMode(params: RequestEvent['params'], cookies: KitCookies): void {
  params[STORAGE_VIEW_MODE_KEY] = cookies.get(STORAGE_VIEW_MODE_KEY);
}
export const enum ViewMode {
  Table = 'table',
  Grid = 'grid',
}
