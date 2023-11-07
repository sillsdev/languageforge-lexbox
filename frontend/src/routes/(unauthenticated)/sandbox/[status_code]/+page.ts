import type { PageLoadEvent } from './$types';

export async function load(event: PageLoadEvent) {
  const statusCode = Number(event.params?.status_code);
  if (statusCode === 403) {
    await event.fetch('/api/AuthTesting/403');
  } else if (statusCode === 500) {
    await event.fetch('/api/testing/test500NoException');
  }

  return { statusCode };
}
