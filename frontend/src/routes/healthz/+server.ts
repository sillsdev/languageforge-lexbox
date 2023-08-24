import { APP_VERSION } from '$lib/util/verstion';
import type { RequestHandler } from './$types'
import { text } from '@sveltejs/kit'

export const GET: RequestHandler = () => {
  return text('Healthy', {
    headers: {
      'lexbox-version': APP_VERSION,
    },
  });
};
