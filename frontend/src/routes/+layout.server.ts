import { APP_VERSION, apiVersion } from '$lib/util/version';

import type { LayoutServerLoadEvent } from './$types'
import { USER_LOAD_KEY } from '$lib/user';
import { getRootTraceparent } from '$lib/otel/otel.server'

// eslint-disable-next-line @typescript-eslint/unbound-method
export async function load({ locals, depends, fetch }: LayoutServerLoadEvent) {
  const user = locals.getUser();
  const traceParent = getRootTraceparent()

  //depend even if we're not currently logged in as later on we won't and we can invalidate if we don't depend on it
  //todo consider moving this and the user related code into the (authenticated) +layout.server.ts file
  depends(USER_LOAD_KEY);
  if (apiVersion.value === null) {
    const response = await fetch('/api/healthz');
    apiVersion.value = response.headers.get('lexbox-version');
  }
  return {
    user,
    activeLocale: locals.activeLocale,
    traceParent,
    serverVersion: APP_VERSION,
    apiVersion: apiVersion.value
  }
}
