import {APP_VERSION} from '$lib/util/version';
import {
  TraceFlags,
  context,
  propagation,
  trace,
  type Context,
  type Span,
} from '@opentelemetry/api';

import {env} from '$env/dynamic/private';
import {getNodeAutoInstrumentations} from '@opentelemetry/auto-instrumentations-node';
import {OTLPTraceExporter} from '@opentelemetry/exporter-trace-otlp-http';
import {resourceFromAttributes} from '@opentelemetry/resources';
import {NodeSDK} from '@opentelemetry/sdk-node';
import {ATTR_HTTP_REQUEST_METHOD, ATTR_HTTP_RESPONSE_HEADER, ATTR_HTTP_RESPONSE_STATUS_CODE, ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION, ATTR_URL_FULL} from '@opentelemetry/semantic-conventions';
import type {RequestEvent, NavigationEvent} from '@sveltejs/kit';
import {
  traceFetch as _traceFetch,
  traceEventAttributes,
  traceHeaders,
  SERVICE_NAME,
  tracer,
} from '.';

export * from '.';

// Span and attribute names and values are based primarily on the OpenTelemetry semantic conventions for HTTP
// https://opentelemetry.io/docs/reference/specification/trace/semantic_conventions/http/

const ROOT_TRACE_PARENT_KEY = 'traceparent';

function buildTraceparent(span: Span): string {
  const spanContext = span.spanContext();
  const sampleDecision = Number(spanContext.traceFlags || TraceFlags.NONE).toString(16);
  return `00-${spanContext.traceId}-${spanContext.spanId}-0${sampleDecision}`;
}

function createTraceparentSpan(event: RequestEvent): Span {
  const method = event.request.method;
  const span = tracer().startSpan(`HTTP ${method}`);
  span.end();
  return span;
}

function buildContextWithTraceparentBaggage(event: RequestEvent): Context {
  // Sometmes the http instrumentation kicks in (so that there's an active span here), but usually it doesn't. I don't understand.
  // So, we just create an empty traceparent span
  const traceparentSpan = trace.getActiveSpan() ?? createTraceparentSpan(event);
  const traceparentContext = trace.setSpan(context.active(), traceparentSpan);
  const traceparent = buildTraceparent(traceparentSpan);
  const traceParentBaggage = propagation.createBaggage({
    [ROOT_TRACE_PARENT_KEY]: {value: traceparent},
  });
  return propagation.setBaggage(traceparentContext, traceParentBaggage);
}

export function getRootTraceparent(): string | undefined {
  const baggage = propagation.getBaggage(context.active());
  return baggage?.getEntry(ROOT_TRACE_PARENT_KEY)?.value;
}

export async function traceRequest(
    event: RequestEvent,
    responseBuilder: (requestSpan: Span) => Response | Promise<Response>,
): Promise<Response> {
  const tracparentContext = buildContextWithTraceparentBaggage(event);
  return context.with(tracparentContext, () => {
    const route = event.route.id;
    const spanName = route ? `${event.request.method} ${route}` : event.request.method;
    return tracer()
        .startActiveSpan(spanName, async (span) => {
          try {
            traceEventAttributes(span, event);
            return await traceResponse(span, () => responseBuilder(span));
          } finally {
            span.end();
          }
        });
  });
}

async function traceResponse(
    span: Span,
    responseBuilder: () => Promise<Response> | Response,
): Promise<Response> {
  const response = await responseBuilder();
  traceResponseAttributes(span, response);
  return response;
}

export async function traceFetch(
    request: Request,
    fetch: () => Promise<Response>,
    event?: RequestEvent | NavigationEvent | Event
): Promise<Response> {
  return _traceFetch([request], () => tracer()
      .startActiveSpan(`${request.method} ${request.url}`, async (span) => {
        try {
          span.setAttributes({
            [ATTR_HTTP_REQUEST_METHOD]: request.method,
            [ATTR_URL_FULL]: request.url,
          });
          traceHeaders(span, 'request', request.headers);
          const traceparent = buildTraceparent(span);
          request.headers.set('Traceparent', traceparent);
          return await traceResponse(span, fetch);
        } finally {
          span.end();
        }
      }), event);
}

function traceResponseAttributes(span: Span, response: Response): void {
  span.setAttribute(ATTR_HTTP_RESPONSE_STATUS_CODE, response.status);
  span.setAttribute(
      ATTR_HTTP_RESPONSE_HEADER('content-length'),
      response.headers.get('Content-Length') ?? 0,
  );
  traceHeaders(span, 'response', response.headers);
}

// Debugging:
// diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.ALL)
// const traceExporter = new ConsoleSpanExporter()
const traceExporter = new OTLPTraceExporter({
  url: env.OTEL_ENDPOINT + '/v1/traces',
});
const sdk = new NodeSDK({
  resource: resourceFromAttributes({
    [ATTR_SERVICE_NAME]: SERVICE_NAME,
    [ATTR_SERVICE_VERSION]: APP_VERSION,
  }),
  traceExporter,
  instrumentations: [
    // Doesn't seem to do anything in our case (except for fs, which is disabled below)
    getNodeAutoInstrumentations({
      '@opentelemetry/instrumentation-fs': {
        // generates hundreds of thousands of spans
        enabled: false,
      },
    }),
  ],
});

sdk.start();

// gracefully shut down the SDK on process exit
process.on('SIGTERM', () => {
  sdk
    .shutdown()
    .then(() => console.log('Tracing terminated'))
    .catch((error) => console.log('Error terminating tracing', error))
    .finally(() => process.exit(0));
});
