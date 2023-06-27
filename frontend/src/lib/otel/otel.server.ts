import {APP_VERSION} from '$lib/util/verstion';
import {
  TraceFlags,
  context,
  propagation,
  trace,
  type Context,
  type Span,
} from '@opentelemetry/api';

import { env } from '$env/dynamic/private';
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { Resource } from '@opentelemetry/resources';
import { NodeSDK } from '@opentelemetry/sdk-node';
import { SemanticAttributes, SemanticResourceAttributes } from '@opentelemetry/semantic-conventions';
import type { RequestEvent } from '@sveltejs/kit';
import {
  traceEventAttributes,
  traceHeaders,
  SERVICE_NAME,
  tracer,
} from '.';

export * from '.';

// Span and attribute names and values are based primarily on the OpenTelemetry semantic conventions for HTTP
// https://opentelemetry.io/docs/reference/specification/trace/semantic_conventions/http/

const ROOT_TRACE_PARENT_KEY = 'traceparent';

const buildTraceparent = (span: Span): string => {
  const spanContext = span.spanContext();
  const sampleDecision = Number(spanContext.traceFlags || TraceFlags.NONE).toString(16);
  return `00-${spanContext.traceId}-${spanContext.spanId}-0${sampleDecision}`;
};

const createTraceparentSpan = (event: RequestEvent): Span => {
  const method = event.request.method;
  const span = tracer().startSpan(`HTTP ${method}`);
  span.end();
  return span;
};

const buildContextWithTraceparentBaggage = (event: RequestEvent): Context => {
  // Sometmes the http instrumentation kicks in (so that there's an active span here), but usually it doesn't. I don't understand.
  // So, we just create an empty traceparent span
  const traceparentSpan = trace.getActiveSpan() ?? createTraceparentSpan(event);
  const traceparentContext = trace.setSpan(context.active(), traceparentSpan);
  const traceparent = buildTraceparent(traceparentSpan);
  const traceParentBaggage = propagation.createBaggage({
    [ROOT_TRACE_PARENT_KEY]: { value: traceparent },
  });
  return propagation.setBaggage(traceparentContext, traceParentBaggage);
};

export const getRootTraceparent = (): string | undefined => {
  const baggage = propagation.getBaggage(context.active());
  return baggage?.getEntry(ROOT_TRACE_PARENT_KEY)?.value;
};

export const traceRequest = async (
  event: RequestEvent,
  responseBuilder: () => Promise<Response>,
): Promise<Response> => {
  const tracparentContext = buildContextWithTraceparentBaggage(event);
  return context.with(tracparentContext, () => {
    const route = event.route.id;
    const spanName = route ? `${event.request.method} ${route}` : event.request.method;
    return tracer()
      .startActiveSpan(spanName, async (span) => {
        try {
          traceEventAttributes(span, event);
          return await traceResponse(span, responseBuilder);
        } finally {
          span.end();
        }
      });
  });
};

const traceResponse = async (
  span: Span,
  responseBuilder: () => Promise<Response> | Response,
): Promise<Response> => {
  const response = await responseBuilder();
  traceResponseAttributes(span, response);
  return response;
};

export const traceFetch = async (
  request: Request,
  fetch: () => Promise<Response>,
): Promise<Response> => {
  return tracer()
    .startActiveSpan(`${request.method} ${request.url}`, async (span) => {
      try {
        span.setAttributes({
          [SemanticAttributes.HTTP_METHOD]: request.method,
          [SemanticAttributes.HTTP_TARGET]: request.url,
        })
        traceHeaders(span, 'request', request.headers);
        const traceparent = buildTraceparent(span);
        request.headers.set('Traceparent', traceparent);
        return await traceResponse(span, fetch);
      } finally {
        span.end();
      }
    });
};

const traceResponseAttributes = (span: Span, response: Response): void => {
  span.setAttribute(SemanticAttributes.HTTP_STATUS_CODE, response.status);
  span.setAttribute(
    SemanticAttributes.HTTP_RESPONSE_CONTENT_LENGTH,
    response.headers.get('Content-Length') ?? 0,
  );
  traceHeaders(span, 'response', response.headers);
};

// Debugging:
// diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.ALL)
// const traceExporter = new ConsoleSpanExporter()
const traceExporter = new OTLPTraceExporter({
  url: env.OTEL_ENDPOINT + '/v1/traces',
});
const sdk = new NodeSDK({
  resource: new Resource({
    [SemanticResourceAttributes.SERVICE_NAME]: SERVICE_NAME,
    [SemanticResourceAttributes.SERVICE_VERSION]: APP_VERSION,
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
