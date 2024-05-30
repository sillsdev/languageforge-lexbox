import type { PageLoadEvent } from './$types';

export function load(event: PageLoadEvent) {
  return {
    appName: event.url.searchParams.get('appName') as string,
    scope: event.url.searchParams.get('scope'),
    postback: JSON.parse(event.url.searchParams.get('postback') ?? '{}') as Record<string, string>
  };
}
