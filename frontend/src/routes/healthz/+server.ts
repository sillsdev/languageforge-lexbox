import { APP_VERSION } from '$lib/util/version';
import type { RequestEvent } from './$types'
import { text } from '@sveltejs/kit'

export function GET(_event: RequestEvent): Response {
  return text('Healthy', {
    headers: {
      'lexbox-version': APP_VERSION,
    },
  });
}
