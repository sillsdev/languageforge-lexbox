import { APP_VERSION } from '$lib/util/version';

//used externally to get the app version
export function GET(): Response {
  return new Response(APP_VERSION);
}
