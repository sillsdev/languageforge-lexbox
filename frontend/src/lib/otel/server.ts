import {
	TraceFlags,
	context,
	propagation,
	trace,
	type Span,
	type Context,
  type Attributes,
} from '@opentelemetry/api';

import { SemanticAttributes } from '@opentelemetry/semantic-conventions';
import { NodeSDK } from '@opentelemetry/sdk-node';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import type { Redirect, RequestEvent } from '@sveltejs/kit';
import { Resource } from '@opentelemetry/resources';
import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions';
import {
	traceEventAttributes,
	traceErrorEvent as _traceErrorEvent,
	traceHeaders,
  type ErrorAttributes,
} from './shared';
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node';
import { env } from '$env/dynamic/private';

// Span and attribute names and values are based primarily on the OpenTelemetry semantic conventions for HTTP
// https://opentelemetry.io/docs/reference/specification/trace/semantic_conventions/http/

const serviceName = 'LexBox-SvelteKit-Server';

export const traceErrorEvent = (error: unknown, event: RequestEvent, metadata: ErrorAttributes): string =>
	_traceErrorEvent(serviceName, error, event, metadata);

const ROOT_TRACE_PARENT_KEY = 'traceparent';

const buildTraceparent = (span: Span): string => {
	const spanContext = span.spanContext();
	const sampleDecision = Number(spanContext.traceFlags || TraceFlags.NONE).toString(16);
	return `00-${spanContext.traceId}-${spanContext.spanId}-0${sampleDecision}`;
};

const createTraceparentSpan = (event: RequestEvent): Span => {
	const method = event.request.method;
	const span = trace.getTracer(serviceName).startSpan(`HTTP ${method}`);
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
		return trace
			.getTracer(serviceName)
			.startActiveSpan(`${event.request.method} ${event.route.id}`, async (span) => {
				try {
          traceEventAttributes(span, event);
					return await responseBuilder();
				} finally {
					span.end();
				}
			});
	});
};

export const traceResponse = async (
	{ method, route }: {method: string, route: string | null},
	responseBuilder: () => Promise<Response> | Response,
): Promise<Response> => {
	return trace
		.getTracer(serviceName)
		.startActiveSpan(
			`${method} ${route} - Response`,
			async (responseSpan) => {
				try {
					const response = await responseBuilder();
					traceResponseAttributes(responseSpan, response);
					return response;
				} catch (error) {
					if (isRedirect(error)) {
						traceRedirectAttributes(responseSpan, error);
					}
					throw error;
				} finally {
					responseSpan.end();
				}
			},
		);
};

export const traceFetch = async (
	request: Request,
	fetch: () => Promise<Response>,
): Promise<Response> => {
	return trace
		.getTracer(serviceName)
		.startActiveSpan(`${request.method} ${request.url}`, async (span) => {
      try {
        span.setAttributes({
          [SemanticAttributes.HTTP_METHOD]: request.method,
          [SemanticAttributes.HTTP_TARGET]: request.url,
        })
        traceHeaders(span, 'request', request.headers);
        const traceparent = buildTraceparent(span);
        request.headers.set('Traceparent', traceparent);
        return await traceResponse({ method: request.method, route: request.url }, fetch);
			} finally {
				span.end();
			}
		});
};

const traceResponseAttributes = (span: Span, response: Response) => {
	span.setAttribute(SemanticAttributes.HTTP_STATUS_CODE, response.status);
	span.setAttribute(
		SemanticAttributes.HTTP_RESPONSE_CONTENT_LENGTH,
		response.headers.get('Content-Length') ?? 0,
	);
	traceHeaders(span, 'response', response.headers);
};

const traceRedirectAttributes = (span: Span, redirect: Redirect) => {
	span.setAttribute('http.redirect_location', redirect.location);
	span.setAttribute(SemanticAttributes.HTTP_STATUS_CODE, redirect.status);
};

const isRedirect = (error: any): error is Redirect => {
	return 'status' in error && 'location' in error;
};

// Debugging:
// diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.ALL)
// const traceExporter = new ConsoleSpanExporter()
const traceExporter = new OTLPTraceExporter({
	url: env.OTEL_ENDPOINT + "/v1/traces",
});
const sdk = new NodeSDK({
	resource: new Resource({
		[SemanticResourceAttributes.SERVICE_NAME]: serviceName,
		[SemanticResourceAttributes.SERVICE_VERSION]: '0.0.1',
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
