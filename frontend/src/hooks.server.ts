import {loadI18n, pickBestLocale} from '$lib/i18n';
import {AUTH_COOKIE_NAME, getUser, isAuthn} from '$lib/user';
import {apiVersion} from '$lib/util/version';
import {redirect, type Handle, type HandleFetch, type HandleServerError, type RequestEvent, type ResolveOptions} from '@sveltejs/kit';
import {ensureErrorIsTraced, traceRequest, traceFetch} from '$lib/otel/otel.server';
import {env} from '$env/dynamic/private';
import {getErrorMessage, validateFetchResponse} from './hooks.shared';
import {setViewMode} from './routes/(authenticated)/shared';
import * as setCookieParser from 'set-cookie-parser';
import {AUTHENTICATED_ROOT, UNAUTHENTICATED_ROOT} from './routes';

const PUBLIC_ROUTE_ROOTS = [
  UNAUTHENTICATED_ROOT,
  'email',
  'healthz',
];

function getRoot(routeId: string): string {
  const [_empty, root] = routeId.split('/');
  return root;
}

async function initI18n(event: RequestEvent): Promise<void> {
  const user = event.locals.getUser();
  const acceptLanguageHeader = event.request.headers.get('Accept-Language');
  // Used for SSR + emails + CSR
  event.locals.activeLocale = pickBestLocale(user?.locale, acceptLanguageHeader);
  await loadI18n();
}

// eslint-disable-next-line func-style, @typescript-eslint/unbound-method
export const handle: Handle = ({event, resolve}) => {
  console.log(`HTTP request: ${event.request.method} ${event.request.url}`);
  event.locals.getUser = () => getUser(event.cookies);
  return traceRequest(event, async () => {
    await initI18n(event);

    const options: ResolveOptions = {
      filterSerializedResponseHeaders: () => true,
    }

    const {cookies, route: {id: routeId}} = event;
    if (!routeId) {
      redirect(307, '/');
    } else if (PUBLIC_ROUTE_ROOTS.includes(getRoot(routeId))) {
      const response = await resolve(event, options);
      if (routeId.endsWith('/logout')) {
        response.headers.set('Clear-Site-Data', '"cache"');
      }
      return response;
    } else if (!isAuthn(cookies)) {
      const relativePath = event.url.href.substring(event.url.origin.length);
      if (relativePath !== '/')
        redirect(307, `/login?ReturnUrl=${encodeURIComponent(relativePath)}`);
      else
        redirect(307, '/login');
    }
    //when at home
    if (routeId == `/${AUTHENTICATED_ROOT}`) {
      setViewMode(event.params, cookies);
    }

    return resolve(event, options);
  });
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
      routeId.endsWith(AUTHENTICATED_ROOT) || routeId.endsWith('/home') || routeId.endsWith('/admin'));

    return response;
  }, event);

  if (response.headers.has('lexbox-version')) {
    apiVersion.value = response.headers.get('lexbox-version');
  }

  const lexBoxSetAuthCookieHeader = response.headers.getSetCookie()
    .find(h => h.startsWith(`${AUTH_COOKIE_NAME}=`));

  if (lexBoxSetAuthCookieHeader) {
    const { name, value, ...options } = setCookieParser.parseString(lexBoxSetAuthCookieHeader);
    const path = options.path ?? '/';
    event.cookies.set(AUTH_COOKIE_NAME, value, {
            ...options,
            path,
            // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-explicit-any
            sameSite: options.sameSite?.toLocaleLowerCase() as any,
          });
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
