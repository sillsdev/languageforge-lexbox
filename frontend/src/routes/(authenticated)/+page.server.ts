import type { PageServerLoadEvent } from './$types';
import { STORAGE_VIEW_MODE_KEY, type ViewMode } from './shared';

export function load(event: PageServerLoadEvent) {
  const projectViewMode = event.cookies.get(STORAGE_VIEW_MODE_KEY) as (ViewMode | undefined);
  return { projectViewMode };
}
