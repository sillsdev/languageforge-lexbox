import { getUser, isAuthn } from '$lib/user'
import { apiVersion } from '$lib/util/verstion';
import { redirect, type Handle, type HandleFetch, type HandleServerError, type ResolveOptions } from '@sveltejs/kit'
import { loadI18n } from '$lib/i18n';
import { ensureErrorIsTraced, traceRequest, traceFetch } from '$lib/otel/otel.server'
import { env } from '$env/dynamic/private';
import { getErrorMessage } from './hooks.shared';

const PUBLIC_ROUTES = [
  '/login',
  '/register',
  '/email',
  '/forgotPassword',
  '/forgotPassword/emailSent',
]

export const handle = (async ({ event, resolve }) => {
  event.locals.getUser = () => getUser(event.cookies);
  return traceRequest(event, async () => {
    await loadI18n();
    const { cookies, url: { pathname } } = event
    const options: ResolveOptions = {
      filterSerializedResponseHeaders: () => true,
    }

    if (PUBLIC_ROUTES.includes(pathname)) {
      return resolve(event, options)
    }

    if (!isAuthn(cookies)) {
      throw redirect(307, '/login')
    }

    return resolve(event, options)
  })
}) satisfies Handle

export const handleFetch = (async ({ event, request, fetch }) => {
  if (env.BACKEND_HOST && request.url.startsWith(event.url.origin + '/api')) {
    const cookie = event.request.headers.get('cookie') as string;
    request.headers.set('cookie', cookie);
    request = new Request(request.url.replace(event.url.origin, env.BACKEND_HOST), request);
  } else {
    console.log('Skipping cookie forwarding for non-backend request', request.url);
  }

  const response = await traceFetch(request, () => fetch(request));
  if (response.headers.has('lexbox-version')) {
    apiVersion.value = response.headers.get('lexbox-version');
  }
  return response;
}) satisfies HandleFetch;

export const handleError: HandleServerError = ({ error, event }) => {
  const handler = 'server-error-hook';
  console.error(handler, error);
  const traceId = ensureErrorIsTraced(error, { event }, { ['app.error.source']: handler });
  const message = getErrorMessage(error);
  return {
    traceId,
    message: `${message} (${traceId})`, // traceId is appended so we have it on error.html
    handler,
  };
};
