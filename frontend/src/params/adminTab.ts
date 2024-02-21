import type { ParamMatcher } from '@sveltejs/kit';
import { adminTabs, type AdminTabId } from '../routes/(authenticated)/admin/[[tab=adminTab]]/AdminTabs.svelte';

export function match(param: string): param is AdminTabId {
  // eslint-disable-next-line
  return adminTabs.includes(param as any);
}

match satisfies ParamMatcher;
