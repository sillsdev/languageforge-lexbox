import type { LexAuthUser } from '$lib/user';
import {
  SpanStatusCode,
  trace,
  type Attributes,
  type Exception,
  type Span,
  type Tracer,
  context,
} from '@opentelemetry/api';
import { SemanticAttributes } from '@opentelemetry/semantic-conventions';
import type { NavigationEvent, RequestEvent } from '@sveltejs/kit';
import { page } from '$app/stores';
import { get } from 'svelte/store';
import { isTraced, type TraceId, isTraceable } from './types';
import { makeOperation, type Exchange, type OperationResult, type AnyVariables, mapExchange } from '@urql/svelte';
import { browser } from '$app/environment';

export const SERVICE_NAME = browser ? 'LexBox-SvelteKit-Client' : 'LexBox-SvelteKit-Server';
const GQL_ERROR_SOURCE = browser ? 'client-gql-error' : 'server-gql-error';

export const tracer = (): Tracer => trace.getTracer(SERVICE_NAME);

type ErrorTracer = ErrorHandler | 'server-gql-error' | 'client-gql-error';
type ErrorAttributes = Attributes & { ['app.error.source']: ErrorTracer };

interface ErrorContext {
  event: RequestEvent | NavigationEvent | Event | undefined;
  span: Span;
}

// Span and attribute names and values are based primarily on the OpenTelemetry semantic conventions for HTTP
// https://opentelemetry.io/docs/reference/specification/trace/semantic_conventions/http/

const normalizeHeaderName = (name: string): string => {
  // https://opentelemetry.io/docs/reference/specification/trace/semantic_conventions/http/#http-request-and-response-headers
  return name.replaceAll('-', '_').toLowerCase();
};

const RECORDED_HEADERS_NORMALIZED = [
  'Accept',
  'Accept-Encoding',
  'Accept-Language',
  'Cache-Control',
  'Pragma',
  'Connection',
  'Content-Length',
  'Contnt-Type',
  'Host',
  'Origin',
  'Referer',
  'Upgrade-Insecure-Requests',
  'User-Agent',
  'Via',
  'ETag',
  'Sec-CH-UA',
  'Sec-CH-UA-Mobile',
  'Sec-CH-UA-Platform',
  'Sec-Fetch-Dest',
  'Sec-Fetch-Mode',
  'Sec-Fetch-Site',
  'Sec-Fetch-User',
  'x_sveltekit_page',
].map(normalizeHeaderName);

export const ensureErrorIsTraced = (
  error: unknown,
  context: Partial<ErrorContext> | undefined,
  metadata: ErrorAttributes,
): TraceId => {
  if (isTraced(error)) {
    return error.traceId;
  }
  return ensureTraced('error', context?.span, (span) =>
    traceErrorEvent(error, { span, event: context?.event }, metadata),
  );
}

const traceErrorEvent = (
  error: unknown,
  context: ErrorContext,
  metadata: ErrorAttributes,
): TraceId => {
  const { span, event } = context;
  span.recordException(error as Exception);
  span.setStatus({ code: SpanStatusCode.ERROR });
  if (metadata) {
    span.setAttributes(metadata);
  }

  if (event) traceEventAttributes(span, event);

  const traceId = span.spanContext().traceId;
  if (isTraceable(error)) {
    error.trace(traceId);
  }

  return traceId;
};

export const traceHeaders = (span: Span, type: 'request' | 'response', headers: Headers): void => {
  headers.forEach((value, key) => {
    const normalizedName = normalizeHeaderName(key);
    if (RECORDED_HEADERS_NORMALIZED.includes(normalizedName)) {
      span.setAttribute(`http.${type}.header.${normalizedName}`, value);
    }
  });
};

function getUser(event: RequestEvent | NavigationEvent | Event): LexAuthUser | null {
  if (isRequestEvent(event)) {
    return event.locals.getUser();
  } else {
    try {
      const data = get(page).data;
      return data.user as LexAuthUser;
    } catch {
      return null;
    }
  }
}

export const traceEventAttributes = (span: Span, event: RequestEvent | NavigationEvent | Event): void => {
  const user = getUser(event);
  if (user) {
    span.setAttribute('app.user.id', user.id);
  }
  if (isBrowserEvent(event)) {
    traceBrowserAttributes(span, window);
  } else {
    const { route, url } = event;
    span.setAttribute(SemanticAttributes.HTTP_ROUTE, route.id as string);
    span.setAttribute(SemanticAttributes.HTTP_URL, url.href);
    span.setAttribute(
      SemanticAttributes.HTTP_TARGET,
      `${url.pathname}${url.hash ?? ''}${url.search ?? ''}`,
    );
    span.setAttribute(SemanticAttributes.HTTP_SCHEME, url.protocol);
    span.setAttribute(SemanticAttributes.NET_HOST_NAME, url.hostname);
    span.setAttribute(SemanticAttributes.NET_HOST_PORT, url.port);
    if (isRequestEvent(event)) {
      const { request } = event;
      span.setAttribute(SemanticAttributes.HTTP_CLIENT_IP, event.getClientAddress());
      span.setAttribute(SemanticAttributes.HTTP_METHOD, request.method);
      span.setAttribute(
        SemanticAttributes.HTTP_REQUEST_CONTENT_LENGTH,
        request.headers.get('Content-Length') ?? 0,
      );
      span.setAttribute(
        SemanticAttributes.HTTP_USER_AGENT,
        request.headers.get('User-Agent') ?? '',
      );
      traceHeaders(span, 'request', request.headers);
      span.setAttribute('app.sveltekit.isDataRequest', event.isDataRequest);
    }
  }
};

const traceBrowserAttributes = (span: Span, window: Window): void => {
  span.setAttributes({
    ['window.location.hostname']: window.location.hostname,
    ['window.location.scheme']: window.location.protocol,
    ['window.location.href']: window.location.href,
    ['window.location.port']: window.location.port,
    ['window.location.path']: window.location.pathname,
    ['window.location.query']: window.location.search,
    ['window.location.hash']: window.location.hash,
    ['window.location.origin']: window.location.origin,
    ['window.navigator.user_agent']: window.navigator.userAgent,
    ['window.navigator.language']: window.navigator.language,
    ['window.navigator.languages']: window.navigator.languages.join(),
  });
};

/**
 * Can be used if it is uncertain wether the caller is in the scope of an active trace.
 * @param name The name to use if a new span needs to be created.
 * @param currSpan The span to trace the error on. Used when explicit propagation is required.
 * If not provided, the tracer will
 * (1) try to find the active span itself or else
 * (2) create its own span.
 * @param instrumentedAction The action to perform in the context of whatever span ends up being used.
 * The action is assumed to do all the necessary tracing and error handling.
 * @returns The trace ID of whatever span ends up being used.
 */
const ensureTraced = (name: string, currSpan: Span | undefined, instrumentedAction: (span: Span) => string): TraceId => {
  const foundSpan = currSpan ?? trace.getActiveSpan();
  if (foundSpan) {
    return instrumentedAction(foundSpan);
  } else {
    return tracer().startActiveSpan(name, (span) => {
      try {
        instrumentedAction(span);
        return span.spanContext().traceId;
      } finally {
        span.end();
      }
    });
  }
};

const isBrowserEvent = (event: RequestEvent | NavigationEvent | Event): event is Event => {
  return 'bubbles' in event;
};

const isRequestEvent = (event: RequestEvent | NavigationEvent | Event): event is RequestEvent => {
  return 'cookies' in event;
};

/**
 * We trace urql operations by both (1) wrapping entire queries and mutations and (2) intercepting operations with the tracingExchange.
 * 1) Wrapping allows us to catch and trace thrown errors.
 * 2) Intercepting gives us more context (e.g. the operation object), allows us to trace subscription setup (I think) and gives us a tidier place to hide the dirty work.
 */

export const traceOperation = <T extends OperationResult<unknown, AnyVariables>>(operation: () => Promise<T>): Promise<T> => {
  return tracer().startActiveSpan('operation', async (span) => {
    try {
      return await operation();
    } catch (error) {
      ensureErrorIsTraced(error, { span }, { ['app.error.source']: GQL_ERROR_SOURCE });
      throw error;
    } finally {
      span.end();
    }
  });
}

const ACTIVE_SPAN_KEY = 'ACTIVE_OTEL_OPERATION_SPAN';
const CACHED_SPAN_KEY = 'CACHED_OTEL_OPERATION_SPAN';

export const tracingExchange: Exchange = mapExchange({
  onOperation: (operation) => {
    const operationSpan = tracer().startSpan(`operation ${operation.kind}`, {
      attributes: { 'graphql.operation.kind': operation.kind }
    });
    const operationSpanContext = trace.setSpan(context.active(), operationSpan);
    return makeOperation(operation.kind, operation, {
      ...operation.context,
      fetch: operation.context.fetch ? context.bind(operationSpanContext, operation.context.fetch) : undefined,
      [ACTIVE_SPAN_KEY]: operationSpan,
    });
  },
  onResult: (result) => {
    const operation = result.operation;
    const cacheOutcome = operation.context.meta?.cacheOutcome;
    const operationSpan = (result.extensions?.[CACHED_SPAN_KEY] ?? operation.context[ACTIVE_SPAN_KEY]) as Span | undefined;
    const operationSpanContext = operationSpan?.spanContext();

    if (result.error && (operation.kind === 'query' || operation.kind === 'subscription')) {
      // queries/subscriptions generally won't lead to "expected" errors, so we trace them all.
      // If something really went wrong we should have already traced it higher up anyway.
      ensureErrorIsTraced(result.error, { span: operationSpan }, { ['app.error.source']: GQL_ERROR_SOURCE });
    }

    if (cacheOutcome && cacheOutcome !== 'miss') {
      // We have no access to the span started above and it will therefore never end.
      // That's alright, because this operation is not particularly interesting.
      // Instead we (should) have access to the span/context of the original/cached operation, so we'll link to that.
      tracer().startActiveSpan(`operation ${operation.kind} cache hit`, {
        links: (operationSpanContext ? [{ context: operationSpanContext }] : [])
      }, (span) => {
        if (!operationSpanContext) { // :'(
          const keys = Object.keys(operation.variables ?? {}).join();
          span.setAttribute('app.urql.cache.missing_original_trace', `${operation.kind}: ${keys} (${operation.key})`);
        }
        span.end();
      });
    } else if (operationSpan) {
      result.extensions ??= {};
      // This is a bit of a hack that's only truly necessary in dev.
      // In prod we get the cached operation, but in dev we only get the cached result, so that's where we need
      // to store stuff if we ever want to see it again.
      // https://github.com/urql-graphql/urql/blob/6a441884790bf6b07acb553f7bdc3702c3a1c315/packages/core/src/exchanges/cache.ts#L78
      result.extensions[CACHED_SPAN_KEY] = operationSpan;
      operationSpan?.end();
    }
  },
});
