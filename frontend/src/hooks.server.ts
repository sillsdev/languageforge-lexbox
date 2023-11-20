import { getUser, isAuthn } from '$lib/user'
import { apiVersion } from '$lib/util/version';
import { redirect, type Handle, type HandleFetch, type HandleServerError, type ResolveOptions } from '@sveltejs/kit'
import { loadI18n } from '$lib/i18n';
import { ensureErrorIsTraced, traceRequest, traceFetch } from '$lib/otel/otel.server'
import { env } from '$env/dynamic/private';
import { getErrorMessage, validateFetchResponse } from './hooks.shared';

const UNAUTHENTICATED_ROOT = '(unauthenticated)';
const AUTHENTICATED_ROOT = '(authenticated)';

const PUBLIC_ROUTE_ROOTS = [
  UNAUTHENTICATED_ROOT,
  'email',
  'healthz',
];

function getRoot(routeId: string): string {
  const [_empty, root] = routeId.split('/');
  return root;
}

// eslint-disable-next-line func-style
export const handle: Handle = ({ event, resolve }) => {
  console.log(`HTTP request: ${event.request.method} ${event.request.url}`);
  event.locals.getUser = () => getUser(event.cookies);
  return traceRequest(event, async () => {
    await loadI18n();

    const options: ResolveOptions = {
      filterSerializedResponseHeaders: () => true,
    }

    const { cookies, route: { id: routeId } } = event;
    if (!routeId) {
      throw redirect(307, '/');
    } else if (PUBLIC_ROUTE_ROOTS.includes(getRoot(routeId))) {
      return resolve(event, options);
    } else if (!isAuthn(cookies)) {
      throw redirect(307, '/login');
    }

    return resolve(event, options);
  })
};

// eslint-disable-next-line func-style
export const handleFetch: HandleFetch = async ({ event, request, fetch }) => {
  if (env.BACKEND_HOST && request.url.startsWith(event.url.origin + '/api')) {
    const cookie = event.request.headers.get('cookie') as string;
    request.headers.set('cookie', cookie);
    request = new Request(request.url.replace(event.url.origin, env.BACKEND_HOST), request);
  } else {
    console.log('Skipping cookie forwarding for non-backend request', request.url);
  }

  const response = await traceFetch(request, async () => {
    const response = await fetch(request);

    const routeId = event.route.id ?? '';
    validateFetchResponse(response,
      routeId.endsWith('/login'),
      routeId.endsWith(AUTHENTICATED_ROOT) || routeId.endsWith('/home') || routeId.endsWith('/admin'));

    return response;
  }, event);

  if (response.headers.has('lexbox-version')) {
    apiVersion.value = response.headers.get('lexbox-version');
  }

  return response;
};

// eslint-disable-next-line func-style
export const handleError: HandleServerError = ({ error, event }) => {
  const handler = 'server-error-hook';
  const traceId = ensureErrorIsTraced(error, { event }, { ['app.error.source']: handler });
  console.error(handler, error, `Trace ID: ${traceId}.`);
  const message = getErrorMessage(error);
  return {
    traceId,
    message: `${message} (${traceId})`, // traceId is appended so we have it on error.html
    handler,
  };
};
