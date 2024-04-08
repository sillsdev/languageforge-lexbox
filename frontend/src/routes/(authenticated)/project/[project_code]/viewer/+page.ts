import type {PageLoadEvent} from './$types';
export const ssr = false; // 💖
export function load(event: PageLoadEvent) {
  return {code: event.params.project_code};
}
