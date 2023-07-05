import type {RequestHandler} from '@sveltejs/kit';
import {APP_VERSION} from '$lib/util/verstion';

//used externally to get the app version
export const GET: RequestHandler = () => {
  return new Response(APP_VERSION);
};
