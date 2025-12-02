import type {Cookies as KitCookies, RequestEvent} from '@sveltejs/kit';

import Cookies from 'js-cookie';
import type {PageLoadEvent} from './$types';
import {browser} from '$app/environment';

export const STORAGE_VIEW_MODE_KEY = 'projectViewMode';

export async function getViewMode(event: PageLoadEvent): Promise<ViewMode | undefined> {
  if (browser) {
    return Cookies.get(STORAGE_VIEW_MODE_KEY) as ViewMode | undefined;
  } else {
    const parentData = await event.parent();
    // stored in locals by hooks.server.ts calling setViewMode
    return parentData.projectViewMode;
  }
}

export function setViewMode(params: RequestEvent, cookies: KitCookies): void {
  params.locals.projectViewMode = cookies.get(STORAGE_VIEW_MODE_KEY) as ViewMode | undefined;
}
export type ViewMode = 'table' | 'grid';
