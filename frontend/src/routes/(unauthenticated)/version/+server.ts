import { APP_VERSION } from '$lib/util/verstion';
import type { RequestHandler } from '@sveltejs/kit';

//used externally to get the app version
export const GET: RequestHandler = () => {
  return new Response(APP_VERSION);
};
