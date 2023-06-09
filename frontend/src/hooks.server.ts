import { isAuthn } from '$lib/user'
import { redirect, type Handle, type HandleFetch, type HandleServerError, type ResolveOptions } from '@sveltejs/kit'
import { storeGqlEndpoint } from '$lib/gql'
import { loadI18n } from '$lib/i18n';
import { traceErrorEvent, traceResponse, traceRequest, traceFetch } from '$lib/otel/server'
import { env } from '$env/dynamic/private';
import { getErrorMessage } from './hooks.shared';

const PUBLIC_ROUTES = [
  '/login',
  '/register',
  '/email',
  '/forgotPassword',
  '/forgotPassword/emailSent',
]

storeGqlEndpoint(env.BACKEND_HOST);

export const handle = (async ({ event, resolve }) => {
  return traceRequest(event, async () => {
    await loadI18n();
    const { cookies, url: { pathname } } = event
    const options: ResolveOptions = {
      filterSerializedResponseHeaders: () => true,
    }

    return traceResponse({ method: event.request.method, route: event.route.id }, () => {
      if (PUBLIC_ROUTES.includes(pathname)) {
        return resolve(event, options)
      }

      if (!isAuthn(cookies)) {
        throw redirect(307, '/login')
      }

      return resolve(event, options)
    })
  })
}) satisfies Handle

export const handleFetch = (async ({ event, request, fetch }) => {
  if (env.BACKEND_HOST && request.url.startsWith(env.BACKEND_HOST)) {
    const cookie = event.request.headers.get('cookie') as string;
    request.headers.set('cookie', cookie);
  } else {
    console.log('Skipping cookie forwarding for non-backend request', request.url);
  }

  return traceFetch(request, () => fetch(request));
}) satisfies HandleFetch;

export const handleError: HandleServerError = ({ error, event }) => {
  const source = 'server-error-hook';
  console.error(source, error);
  const traceId = traceErrorEvent(error, event, { ['app.error.source']: source });
  const message = getErrorMessage(error);
  return {
    traceId,
    message: `${message} (${traceId})`, // traceId is appended so we have it on error.html
    source,
  };
};
